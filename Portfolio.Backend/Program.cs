using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Octokit.GraphQL;
using Portfolio.Backend.Configuration;
using Portfolio.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddMemoryCache();

builder.Services.AddOptions<GitHubConfiguration>()
	.ValidateOnStart()
	.BindConfiguration("GitHub");

builder.Services.AddTransient<ICredentialStore, ConfigurationCredentialStore>();

builder.Services.AddScoped<IConnection>(sp =>
{
	var creds = sp.GetRequiredService<ICredentialStore>();

	return new Connection(new ProductHeaderValue("portfolio-backend"), creds);
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

if (builder.Environment.IsDevelopment())
{
	// Allow CORS for development
	app.UseCors(cors =>
	{
		cors.WithOrigins("https://localhost:3999");
	});
}

app.Run();
