using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Octokit.GraphQL;
using Portfolio.Backend.Configuration;
using Portfolio.Backend.Extensions;
using Portfolio.Backend.Health;
using Portfolio.Backend.Services;
using Portfolio.Backend.Services.Caching;
using Portfolio.Backend.Services.Implementation;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddSingleton<MemoryCacheWrapper>();

builder.Services.AddSingleton<IMemoryCache>(sp => sp.GetRequiredService<MemoryCacheWrapper>());
builder.Services.AddSingleton<IOutputCacheStore>(sp => sp.GetRequiredService<MemoryCacheWrapper>());

builder.Services.AddMemoryCache(options =>
{
	options.TrackStatistics = true;
	options.SizeLimit = 100_000_000;
});

builder.Services.AddResponseCaching();

builder.Services.AddOutputCache(options =>
{
	options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(180);
	options.AddPolicy("health-checks", builder =>
	{
		builder.Cache()
			.Expire(TimeSpan.FromSeconds(90))
			.SetVaryByQuery("simple")
			.SetCacheKeyPrefix("health-check");
	});
});

builder.Services
	.AddHttpClient("gravatar");

builder.Services
	.AddHttpClient("skill-icons");

// Bind the configuration to the classes to their respective sections in the configuration file,
// and validate the configuration on startup
builder.Services.AddOptionsWithValidateOnStart<GitHubConfiguration>()
	.BindConfiguration("GitHub");

builder.Services.AddOptionsWithValidateOnStart<CryptoConfiguration>()
	.BindConfiguration("Crypto");

builder.Services.AddOptionsWithValidateOnStart<AuthenticationConfiguration>()
	.BindConfiguration("Auth");

builder.Services.AddOptionsWithValidateOnStart<EmailConfiguration>()
	.BindConfiguration("Email");

// Add the Octokit credentials store
builder.Services.AddTransient<ICredentialStore, ConfigurationCredentialStore>();

// Add the GitHub GraphQL client
builder.Services.AddScoped<IConnection>(sp =>
{
	var creds = sp.GetRequiredService<ICredentialStore>();

	return new Connection(new ProductHeaderValue("portfolio-backend"), creds);
});

// Add more external service integrations
builder.Services.AddSingleton<IGravatarRetriever, GravatarRetriever>();
builder.Services.AddSingleton<ISkillIconsRetriever, SkillIconsRetriever>();

// Add the cryptographic helper service
builder.Services.AddSingleton<ICryptoHelper, CryptoHelper>();

builder.Services.AddSingleton<IntruderDetector>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<IntruderDetector>());
builder.Services.AddSingleton<IIntruderDetector>(sp => sp.GetRequiredService<IntruderDetector>());

// Add the email provider which sends password reset emails
builder.Services.AddTransient<IResetPasswordEmailProvider, ResetPasswordEmailProvider>();

// Add the email sender, user service and authentication manager
builder.Services.AddScoped<IEmailer, EmailSender>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthenticator, AuthenticationManager>();

builder.Services.AddDbContext<DatabaseContext>(options =>
{
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("postgres"),
		postgres => postgres.EnableRetryOnFailure(3)
	);

	// Enable detailed and sensitive data logging in development mode
	options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
	options.EnableDetailedErrors(builder.Environment.IsDevelopment());
	// Use lazy loading proxies for better performance with navigation properties
	options.UseLazyLoadingProxies();
	// Use the snake case naming convention for better compatibility with PostgreSQL
	options.UseSnakeCaseNamingConvention();
});

// Add JWT Bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		var authConfig = builder.Configuration.GetRequiredSection("Auth").Get<AuthenticationConfiguration>()!;

		options.TokenValidationParameters.ValidIssuer = authConfig.Issuer;
		options.TokenValidationParameters.ValidAudience = authConfig.Audience;

		// For now, we only support symmetric keys
		options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authConfig.GetSecret()));

		options.TokenValidationParameters.ValidateIssuer = true;
		options.TokenValidationParameters.ValidateAudience = true;

		options.AddRefreshTokenValidator();
	});

// Add the health checks
builder.Services.AddHealthChecks()
	// PostgreSQL database
	.AddNpgSql(sp => sp.GetRequiredService<DatabaseContext>().Database.GetDbConnection().ConnectionString, tags: [HealthCheckerTags.Database])
	// The GitHub API
	.AddCheck<GitHubHealth>("GitHub", HealthStatus.Degraded, [HealthCheckerTags.ThirdParty], TimeSpan.FromSeconds(2))
	// The Gravatar API
	.AddCheck<GravatarHealth>("Gravatar", HealthStatus.Degraded, [HealthCheckerTags.ThirdParty, HealthCheckerTags.ImageService], TimeSpan.FromSeconds(2))
	// The Skill Icons API
	.AddCheck<SkillIconsHealth>("Skill Icons", HealthStatus.Degraded, [HealthCheckerTags.ThirdParty, HealthCheckerTags.ImageService], TimeSpan.FromSeconds(2));

// Make sure just the SimpleConsole logger is used
builder.Logging.ClearProviders().AddSimpleConsole();

var app = builder.Build();

// Use the forwarded headers to get the correct client IP address, protocol and host
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

// for debuggging purposes we log the time it took to process a request, plus the request method, path and host
app.Use(async (ctx, next) =>
{
	var start = Stopwatch.GetTimestamp();

	await next();

	var elapsed = Stopwatch.GetElapsedTime(start);

	var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();

	logger.LogDebug("Request '{method}' to '{path}' from host '{host}' took {time}ms", ctx.Request.Method, ctx.Request.Path, ctx.Request.Headers.Host, elapsed.TotalMilliseconds);
});

// Configure the CORS policy
app.UseCors(cors =>
{
	// Get the allowed origins from the configuration
	var corsOrigins = app.Configuration.GetSection("CORS_ALLOWED_ORIGINS").Get<string>();

	// Split the origins by comma and remove empty entries
	var origins = corsOrigins?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];

	// Allow headers, methods and credentials from the allowed CORS_ALLOWED_ORIGINS
	cors.WithOrigins(origins)
		.AllowAnyHeader()
		.AllowAnyMethod()
		.AllowCredentials();
});

app.UseAuthorization();
app.UseResponseCaching();
app.UseOutputCache();
app.MapControllers();

app.AddHealthChecks();

// And provide a default password for the default user
using (var scope = app.Services.CreateScope())
{
	// Ensure the database is up to date. Due to this, the application is not suitable for multiple instance deployments.
	var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
	db.Database.Migrate();

	// If the default user has no password hash
	var user = db.Users.FirstOrDefault(u => u.Id == 1 && u.PasswordHash.Length == 0);
	if (user is not null)
	{
		// Generate a new password
		var crypto = scope.ServiceProvider.GetRequiredService<ICryptoHelper>();
		var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

		var password = crypto.GenerateStrongPassword();

		// Log the new password to be able to log in
		logger.LogWarning("No password hash found for the default user. Generating one: {newPassword}", password);

		// Set the new password hash
		user.PasswordHash = crypto.Hash(password);

		// Save the changes
		db.SaveChanges();
	}
}

app.Run();
