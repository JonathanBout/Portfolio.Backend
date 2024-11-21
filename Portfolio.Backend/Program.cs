using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
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

builder.Services.AddOptionsWithValidateOnStart<GitHubConfiguration>()
	.BindConfiguration("GitHub");

builder.Services.AddOptionsWithValidateOnStart<CryptoConfiguration>()
	.BindConfiguration("Crypto");

builder.Services.AddOptionsWithValidateOnStart<AuthenticationConfiguration>()
	.BindConfiguration("Auth");

builder.Services.AddOptionsWithValidateOnStart<EmailConfiguration>()
	.BindConfiguration("Email");

builder.Services.AddTransient<ICredentialStore, ConfigurationCredentialStore>();

builder.Services.AddScoped<IConnection>(sp =>
{
	var creds = sp.GetRequiredService<ICredentialStore>();

	return new Connection(new ProductHeaderValue("portfolio-backend"), creds);
});

builder.Services.AddTransient<IResetPasswordEmailProvider, ResetPasswordEmailProvider>();

builder.Services.AddScoped<ICryptoHelper, CryptoHelper>();
builder.Services.AddScoped<IEmailer, EmailSender>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthenticator, AuthenticationManager>();

builder.Services.AddSingleton<IGravatarRetriever, GravatarRetriever>();
builder.Services.AddSingleton<ISkillIconsRetriever, SkillIconsRetriever>();

builder.Services.AddDbContext<DatabaseContext>(options =>
{
	options.UseNpgsql(builder.Configuration.GetConnectionString("postgres"), postgres =>
	{
		postgres.EnableRetryOnFailure(3);
	});
	options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
	options.EnableDetailedErrors(builder.Environment.IsDevelopment());
	options.UseLazyLoadingProxies();
	options.UseSnakeCaseNamingConvention();
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters.ValidIssuer = builder.Configuration["Auth:Issuer"];
		options.TokenValidationParameters.ValidAudience = builder.Configuration["Auth:Audience"];
		options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Auth:Secret"] ?? ""));

		options.TokenValidationParameters.ValidateIssuer = true;
		options.TokenValidationParameters.ValidateAudience = true;

		options.AddRefreshTokenValidator();
	});

builder.Services.AddHealthChecks()
	.AddNpgSql(sp => sp.GetRequiredService<DatabaseContext>().Database.GetDbConnection().ConnectionString, tags: [HealthCheckerTags.Database])
	.AddCheck<GitHubHealth>("GitHub", HealthStatus.Degraded, [HealthCheckerTags.ThirdParty], TimeSpan.FromSeconds(2))
	.AddCheck<GravatarHealth>("Gravatar", HealthStatus.Degraded, [HealthCheckerTags.ThirdParty, HealthCheckerTags.ImageService], TimeSpan.FromSeconds(2))
	.AddCheck<SkillIconsHealth>("Skill Icons", HealthStatus.Degraded, [HealthCheckerTags.ThirdParty, HealthCheckerTags.ImageService], TimeSpan.FromSeconds(2));

builder.Logging.ClearProviders().AddSimpleConsole();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.Use(async (ctx, next) =>
{
	var start = Stopwatch.GetTimestamp();

	await next();

	var elapsed = Stopwatch.GetElapsedTime(start);

	var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();

	logger.LogInformation("Request '{method}' to '{path}' from host '{host}' took {time}ms", ctx.Request.Method, ctx.Request.Path, ctx.Request.Headers.Origin, elapsed.TotalMilliseconds);
});

app.UseCors(cors =>
{
	var corsOrigins = app.Configuration.GetSection("CORS_ALLOWED_ORIGINS").Get<string>();

	var origins = corsOrigins?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];

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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});


using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
	db.Database.Migrate();

	var user = db.Users.First(u => u.Id == 1);

	if (user.PasswordHash is not { Length: > 0 })
	{
		var crypto = scope.ServiceProvider.GetRequiredService<ICryptoHelper>();
		var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

		var password = crypto.GenerateStrongPassword();

		logger.LogWarning("No password hash found for the default user. Generating one: {newPassword}", password);

		user.PasswordHash = crypto.Hash(password);

		db.SaveChanges();
	}
}

app.Run();
