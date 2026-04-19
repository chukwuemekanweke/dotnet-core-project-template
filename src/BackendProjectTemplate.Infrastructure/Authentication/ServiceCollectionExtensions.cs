using System.Text;
using BackendProjectTemplate.Domain.Common.Caching;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
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

        return services;
    }

    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GoogleAuthenticationOptions>(configuration.GetSection(GoogleAuthenticationOptions.SectionName));
        services.Configure<RefreshTokenOptions>(configuration.GetSection(RefreshTokenOptions.SectionName));
        services.AddScoped<IAccessTokenService, JwtTokenGenerator>();
        services.AddSingleton<IAccessTokenRevocationService, AccessTokenRevocationService>();
        services.AddScoped<IAuthenticationIdentityService, IdentityUserService>();
        services.AddScoped<IGoogleIdentityTokenService, GoogleIdentityTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ITwoFactorOtpService, TwoFactorOtpService>();
        services.AddScoped<IOtpDeliveryService, LoggingOtpDeliveryService>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
        services.AddSingleton<IAuthorizationHandler, ActiveSessionAuthorizationHandler>();

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
        });

        return services;
    }
}
