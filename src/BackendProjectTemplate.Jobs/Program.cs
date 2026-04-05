using BackendProjectTemplate.Infrastructure.Messaging;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<WorkerReadinessState>();
builder.Services.Configure<OutboxProcessingOptions>(builder.Configuration.GetSection(OutboxProcessingOptions.SectionName));
builder.Services.AddSqlServerPersistence(builder.Configuration);
builder.Services.AddTransactionalOutbox();
builder.Services.AddHostedService<Worker>();
builder.Services
    .AddHealthChecks()
    .AddCheck<JobsReadinessHealthCheck>(
        "jobs-readiness",
        tags: ["readiness"])
    .AddCheck<JobsLivenessHealthCheck>(
        "jobs-liveness",
        tags: ["liveness"]);
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("BackendProjectTemplate.Jobs"))
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
    Service = "BackendProjectTemplate.Jobs",
    Status = "Running"
}))
.ExcludeFromDescription();

await app.RunAsync();

public partial class Program;
