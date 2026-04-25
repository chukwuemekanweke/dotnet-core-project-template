using Asp.Versioning;
using BackendProjectTemplate.Application;
using BackendProjectTemplate.Infrastructure.Authentication;
using BackendProjectTemplate.Infrastructure.Caching;
using BackendProjectTemplate.Infrastructure.Messaging;
using BackendProjectTemplate.Infrastructure.Observability;
using BackendProjectTemplate.Infrastructure.Payments;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.Infrastructure.Storage;
using BackendProjectTemplate.WebAPI.Features.Authentication.Registrations;
using BackendProjectTemplate.WebAPI.Infrastructure.ApiDocumentation;
using BackendProjectTemplate.WebAPI.Infrastructure;
using BackendProjectTemplate.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<SignUpValidator>();
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddApiDocumentation();
builder.Services.AddApplication();
builder.Services.AddSqlServerWritePersistence(builder.Configuration);
builder.Services.AddSqlServerReadPersistence(builder.Configuration);
builder.Services.AddIdentityUserManagement(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRequestRateLimiting(builder.Configuration);
builder.Services.AddRedisCaching(builder.Configuration);
builder.Services.AddObjectStorage(builder.Configuration);
builder.Services.AddPaymentServices(builder.Configuration);
builder.Services.AddTransactionalOutbox();
builder.Services.AddCustomTelemetryContext();
builder.Services.AddBackendTelemetry(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<CurrentActorMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

app.UseApiDocumentation();
app.MapPrometheusScrapingEndpoint("/metrics").DisableRateLimiting();
app.MapHealthChecks("/health").DisableRateLimiting();
app.MapControllers();
app.MapGet("/", () => TypedResults.Ok(new
{
    Service = "BackendProjectTemplate.WebAPI",
    Status = "Healthy"
}))
.ExcludeFromDescription();

if (app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
{
    await app.InitializeDatabaseAsync();
}

app.Run();

public partial class Program;
