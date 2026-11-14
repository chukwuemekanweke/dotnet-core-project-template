using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Infrastructure.Authorization;
using BackendProjectTemplate.Infrastructure.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services) =>
        services.AddAuthenticationServices(new ConfigurationBuilder().Build());

    public static IServiceCollection AddIdentityUserManagement(this IServiceCollection services, IConfiguration configuration)
    {
        var lockoutOptions = configuration
            .GetSection(AuthenticationLockoutOptions.SectionName)
            .Get<AuthenticationLockoutOptions>() ?? new AuthenticationLockoutOptions();

        lockoutOptions.Validate();
        var passkeyOptions = configuration
            .GetSection(PasskeyAuthenticationOptions.SectionName)
            .Get<PasskeyAuthenticationOptions>() ?? new PasskeyAuthenticationOptions();
        passkeyOptions.Validate();

        services.Configure<AuthenticationLockoutOptions>(configuration.GetSection(AuthenticationLockoutOptions.SectionName));
        services.Configure<AdministratorBootstrapOptions>(
            configuration.GetSection(AdministratorBootstrapOptions.SectionName));

        services
            .AddDataProtection()
            .SetApplicationName("BackendProjectTemplate");

        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
                options.SignIn.RequireConfirmedEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = lockoutOptions.MaxFailedAttempts;
                options.Lockout.DefaultLockoutTimeSpan = lockoutOptions.Duration;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<Persistence.AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IUserPermissionService, IdentityUserPermissionService>();
        services.AddScoped<IApplicationAuthorizationSeeder, ApplicationAuthorizationSeeder>();

        var allowedOrigins = passkeyOptions.AllowedOrigins
            .Select(origin => new Uri(origin).GetLeftPart(UriPartial.Authority))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        services.Configure<IdentityPasskeyOptions>(options =>
        {
            options.ServerDomain = passkeyOptions.RelyingPartyId;
            options.AuthenticatorTimeout = passkeyOptions.AuthenticatorTimeout;
            options.ValidateOrigin = context => ValueTask.FromResult(
                !context.CrossOrigin && allowedOrigins.Contains(context.Origin));
        });
        services.Configure<PasskeyAuthenticationOptions>(configuration.GetSection(PasskeyAuthenticationOptions.SectionName));
        services.AddHttpContextAccessor();

        return services;
    }

    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var googleOptions = configuration
            .GetSection(GoogleAuthenticationOptions.SectionName)
            .Get<GoogleAuthenticationOptions>() ?? new GoogleAuthenticationOptions();
        googleOptions.Validate();
        var twoFactorOptions = configuration
            .GetSection(TwoFactorAuthenticationOptions.SectionName)
            .Get<TwoFactorAuthenticationOptions>() ?? new TwoFactorAuthenticationOptions();
        twoFactorOptions.Validate();

        services.Configure<GoogleAuthenticationOptions>(configuration.GetSection(GoogleAuthenticationOptions.SectionName));
        services.Configure<TwoFactorAuthenticationOptions>(configuration.GetSection(TwoFactorAuthenticationOptions.SectionName));
        services.Configure<RefreshTokenOptions>(configuration.GetSection(RefreshTokenOptions.SectionName));
        services.AddSingleton<IUserAgentParserService, UserAgentParserService>();
        services.AddScoped<IIpAddressResolver, IpAddressResolver>();
        services.AddScoped<IAuthenticationSessionService, AuthenticationSessionService>();

        services.AddScoped<IAccessTokenService, JwtTokenGenerator>();
        services.AddSingleton<IAccessTokenRevocationService, AccessTokenRevocationService>();
        services.AddScoped<IAuthenticationIdentityService, IdentityUserService>();
        services.AddScoped<IPasskeyService, PasskeyService>();
        services.AddScoped<IGoogleIdentityTokenService, GoogleIdentityTokenService>();
        services.AddScoped<IGoogleAuthenticationFlowService, GoogleAuthenticationFlowService>();
        services.AddScoped<ITwoFactorChallengeService, TwoFactorChallengeService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ITwoFactorOtpService, TwoFactorOtpService>();

        return services;
    }

    public static IServiceCollection AddIpGeolocationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var ipApiComOptions = configuration
            .GetSection(IpApiComOptions.SectionName)
            .Get<IpApiComOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{IpApiComOptions.SectionName}' is required.");
        var ipInfoOptions = configuration
            .GetSection(IpInfoOptions.SectionName)
            .Get<IpInfoOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{IpInfoOptions.SectionName}' is required.");
        var ipWhoIsOptions = configuration
            .GetSection(IpWhoIsOptions.SectionName)
            .Get<IpWhoIsOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{IpWhoIsOptions.SectionName}' is required.");

        ipApiComOptions.Validate();
        ipInfoOptions.Validate();
        ipWhoIsOptions.Validate();

        services.Configure<IpApiComOptions>(configuration.GetSection(IpApiComOptions.SectionName));
        services.Configure<IpInfoOptions>(configuration.GetSection(IpInfoOptions.SectionName));
        services.Configure<IpWhoIsOptions>(configuration.GetSection(IpWhoIsOptions.SectionName));

        services.AddHttpClient(HttpClientNames.IpApiCom, client =>
        {
            client.BaseAddress = new Uri(ipApiComOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(HttpRetryPolicies.CreateTransientRetryPolicy());
        services.AddHttpClient(HttpClientNames.IpInfo, client =>
        {
            client.BaseAddress = new Uri(ipInfoOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(HttpRetryPolicies.CreateTransientRetryPolicy());
        services.AddHttpClient(HttpClientNames.IpWhoIs, client =>
        {
            client.BaseAddress = new Uri(ipWhoIsOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(HttpRetryPolicies.CreateTransientRetryPolicy());

        services.AddSingleton<IIpGeolocationProvider, IpApiComClient>();
        services.AddSingleton<IIpGeolocationProvider, IpInfoClient>();
        services.AddSingleton<IIpGeolocationProvider, IpWhoIsClient>();
        services.AddSingleton<IIpGeolocationService, IpGeolocationService>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
        services.AddScoped<IAuthorizationHandler, ActiveSessionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicyNames.RequireActiveSession,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ActiveSessionRequirement()));

            foreach (var permission in ApplicationPermissions.All)
            {
                options.AddPolicy(
                    PermissionPolicyNames.For(permission),
                    policy => policy
                        .RequireAuthenticatedUser()
                        .AddRequirements(
                            new ActiveSessionRequirement(),
                            new PermissionRequirement(permission)));
            }
        });

        return services;
    }
}
