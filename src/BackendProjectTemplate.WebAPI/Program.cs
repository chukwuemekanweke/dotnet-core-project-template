using Asp.Versioning;
using BackendProjectTemplate.Application;
using BackendProjectTemplate.Infrastructure.Authentication;
using BackendProjectTemplate.Infrastructure.Caching;
using BackendProjectTemplate.Infrastructure.Messaging;
using BackendProjectTemplate.Infrastructure.Observability;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddControllers();
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
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddSqlServerPersistence(builder.Configuration);
builder.Services.AddIdentityUserManagement();
builder.Services.AddAuthenticationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRedisCaching(builder.Configuration);
builder.Services.AddTransactionalOutbox();
builder.Services.AddCustomTelemetryContext();
builder.Services.AddBackendTelemetry(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapPrometheusScrapingEndpoint("/metrics");
app.MapHealthChecks("/health");
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
