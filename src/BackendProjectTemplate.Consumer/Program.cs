using BackendProjectTemplate.Consumer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<WorkerReadinessState>();
builder.Services.AddHostedService<Worker>();
builder.Services
    .AddHealthChecks()
    .AddCheck<ConsumerReadinessHealthCheck>(
        "consumer-readiness",
        tags: ["readiness"])
    .AddCheck<ConsumerLivenessHealthCheck>(
        "consumer-liveness",
        tags: ["liveness"]);
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("BackendProjectTemplate.Consumer"))
    .WithTracing(tracing =>
    {
        tracing.AddHttpClientInstrumentation();

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();
    });

var app = builder.Build();

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
