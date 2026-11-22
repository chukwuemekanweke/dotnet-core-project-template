namespace BackendProjectTemplate.WebAPI.Infrastructure.CrossOrigin;

public static class ServiceCollectionExtensions
{
    // Kept for Production hosts that do not configure Cors:AllowedOrigins yet.
    private static readonly string[] DefaultProductionOrigins = ["http://localhost:9000", "https://localhost:9000"];

    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration.GetSection(CrossOriginOptions.SectionName).Get<CrossOriginOptions>()?.AllowedOrigins ?? [];
        var origins = configured
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeOrigin)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (origins.Length == 0)
        {
            origins = DefaultProductionOrigins;
        }

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (environment.IsProduction())
                {
                    policy
                        .WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    return;
                }

                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    private static string NormalizeOrigin(string value)
    {
        var trimmed = value.Trim().TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || uri.AbsolutePath != "/"
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException(
                $"Configuration value '{CrossOriginOptions.SectionName}:{nameof(CrossOriginOptions.AllowedOrigins)}' contains an invalid origin '{value}'. Use scheme://host[:port] with no path.");
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }
}
