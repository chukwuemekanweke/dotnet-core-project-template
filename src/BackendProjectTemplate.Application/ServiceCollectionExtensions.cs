using BackendProjectTemplate.Application.ReferenceData.Features.GetCountries;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<Authentication.Features.SignUp.SignUpHandler>();
        services.AddScoped<Authentication.Features.SignUpOtp.SignUpOtpHandler>();
        services.AddScoped<Authentication.Features.SignIn.SignInHandler>();
        services.AddScoped<Authentication.Features.RequestPasswordReset.RequestPasswordResetHandler>();
        services.AddScoped<Stakeholders.Features.UploadAvatar.UploadAvatarHandler>();
        services.AddScoped<Stakeholders.Features.UpdateProfile.UpdateProfileHandler>();
        services.AddScoped<Providers.Features.ActivateProvider.ActivateProviderHandler>();
        services.AddScoped<GetCountriesHandler>();

        return services;
    }
}
