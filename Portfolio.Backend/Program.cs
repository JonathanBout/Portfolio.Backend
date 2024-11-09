using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Octokit.GraphQL;
using Portfolio.Backend.Configuration;
using Portfolio.Backend.Extensions;
using Portfolio.Backend.Services;
using Portfolio.Backend.Services.Implementation;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddMemoryCache();

builder.Services.AddResponseCaching();

builder.Services.AddHttpClient("gravatar");

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

builder.Services.AddScoped<ICryptoHelper, CryptoHelper>();
builder.Services.AddScoped<IEmailer, EmailSender>();
builder.Services.AddTransient<IResetPasswordEmailProvider, ResetPasswordEmailProvider>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthenticator, AuthenticationManager>();
builder.Services.AddSingleton<IGravatarRetriever, GravatarRetriever>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
app.Use(async (ctx, next) =>
{
	var ts = Stopwatch.GetTimestamp();

	await next();

	var elapsed = Stopwatch.GetElapsedTime(ts);

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
app.MapControllers();

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
