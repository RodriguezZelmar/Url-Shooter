using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using UrlShortener.Api.ErrorHandling;
using UrlShortener.Application;
using UrlShortener.Application.Options;
using UrlShortener.Infrastructure;
using UrlShortener.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<UrlShortenerOptions>().BindConfiguration(UrlShortenerOptions.SectionName);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Validation error keys use the JSON property names ("url"), matching the errors raised by the use cases.
builder.Services.AddControllers(options =>
    options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "URL Shortener API v1");
        options.DocumentTitle = "URL Shortener API";
    });
}

app.MapControllers();

app.Run();

// Exposed so the integration tests can reference the entry point through WebApplicationFactory<Program>.
public partial class Program;
