using System;
using Cms.BatCave.Sonar.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cms.BatCave.Sonar;

public static class Program {
  public static void Main(String[] args) {
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Services.AddControllers();
    // Enable OpenAPI documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Register all Configuration Option Classes for dependency injection
    new ConfigurationDependencyRegistration(builder.Configuration).RegisterDependencies(builder.Services);

    builder.WebHost.UseUrls("http://localhost:8081");

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseSwagger(options => {
      options.RouteTemplate = "api/doc/{documentName}/open-api.{json|yaml}";
    });
    if (app.Environment.IsDevelopment()) {
      app.UseSwaggerUI(options => {
        options.SwaggerEndpoint("/api/doc/v1/open-api.json", "v1");
        options.RoutePrefix = "api/doc-ui";
      });
    }

    app.UseAuthorization();
    // Route requests based on Controller attribute annotations
    app.MapControllers();

    app.Run();
  }
}
