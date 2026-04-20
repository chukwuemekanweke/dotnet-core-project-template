using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerWritePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentActorAccessor, CurrentActorAccessor>();
        services.AddScoped<ICurrentActor>(serviceProvider => serviceProvider.GetRequiredService<ICurrentActorAccessor>());
        services.AddScoped<AuditAndSoftDeleteInterceptor>();
        services.AddScoped<ObservabilityCommandInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseSqlServer(GetRequiredConnectionString(configuration, "SqlServerWrite"))
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditAndSoftDeleteInterceptor>(),
                    serviceProvider.GetRequiredService<ObservabilityCommandInterceptor>()));

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IAppUserStakeholderRepository, AppUserStakeholderRepository>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());

        return services;
    }

    public static IServiceCollection AddSqlServerReadPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ObservabilityCommandInterceptor>();

        services.AddDbContext<AppReadDbContext>((serviceProvider, options) =>
            options.UseSqlServer(GetReadConnectionString(configuration))
                .AddInterceptors(serviceProvider.GetRequiredService<ObservabilityCommandInterceptor>()));

        services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));
        services.AddScoped<IStakeholderReadModelRepository, StakeholderReadModelRepository>();

        return services;
    }

    public static IServiceCollection AddSqlServerPersistence(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddSqlServerWritePersistence(configuration)
            .AddSqlServerReadPersistence(configuration);

    private static string GetReadConnectionString(IConfiguration configuration) =>
        GetRequiredConnectionString(configuration, "SqlServerRead");

    private static string GetRequiredConnectionString(IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name)
        ?? throw new InvalidOperationException($"Connection string '{name}' is required.");
}
