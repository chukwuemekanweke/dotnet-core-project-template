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
        services.AddScoped<GetCountriesHandler>();

        return services;
    }
}
