using BackendProjectTemplate.Application.Identity.Features.SignIn;
using BackendProjectTemplate.Application.Identity.Features.SignUp;
using BackendProjectTemplate.Application.Identity.Features.SignUpOtp;
using BackendProjectTemplate.Application.ReferenceData.Features.GetCountries;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<SignUpValidator>();
        services.AddScoped<SignUpHandler>();
        services.AddScoped<SignUpOtpHandler>();
        services.AddScoped<SignInHandler>();
        services.AddScoped<GetCountriesHandler>();

        return services;
    }
}
