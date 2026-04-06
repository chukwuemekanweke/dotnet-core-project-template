using BackendProjectTemplate.Consumer;
using BackendProjectTemplate.Infrastructure.Authentication;
using BackendProjectTemplate.Infrastructure.Caching;
using BackendProjectTemplate.Infrastructure.Observability;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<WorkerReadinessState>();
builder.Services.AddSqlServerPersistence(builder.Configuration);
builder.Services.AddIdentityUserManagement(builder.Configuration);
builder.Services.AddAuthenticationServices();
builder.Services.AddRedisCaching(builder.Configuration);
builder.Services.AddSubscribers(builder.Configuration);
builder.Services.AddCustomTelemetryContext();
builder.Services
    .AddHealthChecks()
    .AddCheck<ConsumerReadinessHealthCheck>(
        "consumer-readiness",
        tags: ["readiness"])
    .AddCheck<ConsumerLivenessHealthCheck>(
        "consumer-liveness",
        tags: ["liveness"]);
builder.Services.AddBackendTelemetry(builder.Configuration);

var app = builder.Build();

app.MapPrometheusScrapingEndpoint("/metrics");
app.MapHealthChecks("/health/readiness", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("readiness")
});

app.MapHealthChecks("/health/liveness", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("liveness")
});

app.MapGet("/", () => Results.Ok(new
{
    Service = "BackendProjectTemplate.Consumer",
    Status = "Running"
}))
.ExcludeFromDescription();

await app.RunAsync();

public partial class Program;
