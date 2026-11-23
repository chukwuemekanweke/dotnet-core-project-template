using BackendProjectTemplate.Domain.Analytics.ReadModels;
using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Authentication.ReadModels;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Kyc.ReadModels;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.ReadModels;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
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
        services.AddSingleton<SubscriptionConflictTranslationInterceptor>();
        services.AddSingleton<KycConflictTranslationInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(GetRequiredConnectionString(configuration, "PostgresWrite"))
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditAndSoftDeleteInterceptor>(),
                    serviceProvider.GetRequiredService<ObservabilityCommandInterceptor>(),
                    serviceProvider.GetRequiredService<SubscriptionConflictTranslationInterceptor>(),
                    serviceProvider.GetRequiredService<KycConflictTranslationInterceptor>()));

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());
        services.AddScoped<IBillingAccountResolver, BillingAccountResolver>();
        services.AddScoped<ISubscriptionLifecyclePersistence, SubscriptionLifecyclePersistence>();
        services.AddScoped<AdminUserMutationExecutor>();
        services.AddScoped<AdministratorSafetyGuard>();
        services.AddScoped<IAdminUserAccountAdministration, AdminUserAccountAdministration>();
        services.AddScoped<IAdminUserSessionAdministration, AdminUserSessionAdministration>();
        services.AddScoped<IAdminUserRoleAdministration, AdminUserRoleAdministration>();

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
        services.AddScoped<IPaymentAdministrationReadModelRepository, PaymentAdministrationReadModelRepository>();
        services.AddScoped<IKycAdministrationReadModelRepository, KycAdministrationReadModelRepository>();
        services.AddScoped<IPaymentAnalyticsReadModelRepository, PaymentAnalyticsReadModelRepository>();
        services.AddScoped<ILoginActivityReadModelRepository, LoginActivityReadModelRepository>();
        services.AddScoped<IActiveSessionReadModelRepository, ActiveSessionReadModelRepository>();
        services.AddScoped<ISubscriptionAdministrationReadModelRepository, SubscriptionAdministrationReadModelRepository>();
        services.AddScoped<ICustomerSubscriptionReadModelRepository, CustomerSubscriptionReadModelRepository>();
        services.AddScoped<ISubscriptionAnalyticsReadModelRepository, SubscriptionAnalyticsReadModelRepository>();
        services.AddScoped<IAdminUserReadModelRepository, AdminUserReadModelRepository>();
        services.AddScoped<IRegistrationAnalyticsReadModelRepository, RegistrationAnalyticsReadModelRepository>();
        services.AddScoped<IActiveUserAnalyticsReadModelRepository, ActiveUserAnalyticsReadModelRepository>();

        return services;
    }

    public static IServiceCollection AddPostgresPersistence(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddPostgresWritePersistence(configuration)
            .AddPostgresReadPersistence(configuration);

    private static string GetReadConnectionString(IConfiguration configuration) =>
        GetRequiredConnectionString(configuration, "PostgresRead");

    private static string GetRequiredConnectionString(IConfiguration configuration, string name) =>
        PostgresConnectionString.Normalize(
            configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' is required."));
}
