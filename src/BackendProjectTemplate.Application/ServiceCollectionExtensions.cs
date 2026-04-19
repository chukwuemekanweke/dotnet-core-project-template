using BackendProjectTemplate.Application.Authentication.AppUserStakeholders;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;
using BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;
using BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;
using BackendProjectTemplate.Application.Authentication.Features.LogoutSession;
using BackendProjectTemplate.Application.Authentication.Features.RefreshSession;
using BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Application.Providers.Features.ActivateProvider;
using BackendProjectTemplate.Application.ReferenceData.Features.GetCountries;
using BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;
using BackendProjectTemplate.Application.Stakeholders.Features.UploadAvatar;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AppUserStakeholderResolver>();
        services.AddScoped<GoogleSignUpHandler>();
        services.AddScoped<GoogleSignInHandler>();
        services.AddScoped<CompletePasswordResetHandler>();
        services.AddScoped<LogoutSessionHandler>();
        services.AddScoped<RefreshSessionHandler>();
        services.AddScoped<SignUpHandler>();
        services.AddScoped<SignUpOtpHandler>();
        services.AddScoped<SignInHandler>();
        services.AddScoped<RequestPasswordResetHandler>();
        services.AddScoped<UploadAvatarHandler>();
        services.AddScoped<UpdateProfileHandler>();
        services.AddScoped<ActivateProviderHandler>();
        services.AddScoped<GetCountriesHandler>();

        return services;
    }
}
