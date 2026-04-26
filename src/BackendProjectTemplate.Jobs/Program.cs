using BackendProjectTemplate.Application;
using BackendProjectTemplate.Jobs.Authentication;
using BackendProjectTemplate.Infrastructure.Messaging;
using BackendProjectTemplate.Infrastructure.Payments;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.Jobs.HealthChecks;
using BackendProjectTemplate.Jobs.Infrastructure.BackgroundServices;
using BackendProjectTemplate.Jobs.Observability;
using BackendProjectTemplate.Jobs.OutboxProcessing;
using BackendProjectTemplate.Jobs.Payments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(options =>
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddBackgroundServiceReadinessTracking();
builder.Services.AddApplication();
builder.Services.AddPostgresWritePersistence(builder.Configuration);
builder.Services.AddPostgresReadPersistence(builder.Configuration);
builder.Services.AddPaymentServices(builder.Configuration);
builder.Services.AddTransactionalOutbox();
builder.Services.AddRabbitMqOutboxDispatching(builder.Configuration);
builder.Services.AddOutboxMessageProcessing(builder.Configuration);
builder.Services.AddIpAddressLocationEnrichment(builder.Configuration);
builder.Services.AddPaymentReconciliation(builder.Configuration);
builder.Services.AddJobsHealthChecks();
builder.Services.AddJobsOpenTelemetry(builder.Configuration);

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
