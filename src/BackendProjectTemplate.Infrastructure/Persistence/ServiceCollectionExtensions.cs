using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Payments.ReadModels;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresWritePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentActorAccessor, CurrentActorAccessor>();
        services.AddScoped<ICurrentActor>(serviceProvider => serviceProvider.GetRequiredService<ICurrentActorAccessor>());
        services.AddScoped<AuditAndSoftDeleteInterceptor>();
        services.AddScoped<ObservabilityCommandInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(GetRequiredConnectionString(configuration, "PostgresWrite"))
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditAndSoftDeleteInterceptor>(),
                    serviceProvider.GetRequiredService<ObservabilityCommandInterceptor>()));

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());

        return services;
    }

    public static IServiceCollection AddPostgresReadPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ObservabilityCommandInterceptor>();

        services.AddDbContext<AppReadDbContext>((serviceProvider, options) =>
            options.UseNpgsql(GetReadConnectionString(configuration))
                .AddInterceptors(serviceProvider.GetRequiredService<ObservabilityCommandInterceptor>()));

        services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));
        services.AddScoped<IStakeholderReadModelRepository, StakeholderReadModelRepository>();
        services.AddScoped<IWalletTransactionReadModelRepository, WalletTransactionReadModelRepository>();

        return services;
    }

    public static IServiceCollection AddPostgresPersistence(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddPostgresWritePersistence(configuration)
            .AddPostgresReadPersistence(configuration);

    private static string GetReadConnectionString(IConfiguration configuration) =>
        GetRequiredConnectionString(configuration, "PostgresRead");

    private static string GetRequiredConnectionString(IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name)
        ?? throw new InvalidOperationException($"Connection string '{name}' is required.");
}
