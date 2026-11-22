using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.KeyPerFile;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BackendProjectTemplate.Infrastructure.Configuration;

public static class ProductionConfigurationExtensions
{
    public const string DefaultSecretsDirectory = "/run/secrets/app";

    public static TBuilder AddProductionSecrets<TBuilder>(
        this TBuilder builder,
        string applicationName,
        IEnumerable<ProductionSecretRequirement> requirements)
        where TBuilder : IHostApplicationBuilder =>
        builder.AddProductionSecrets(applicationName, requirements, DefaultSecretsDirectory);

    public static TBuilder AddProductionSecrets<TBuilder>(
        this TBuilder builder,
        string applicationName,
        IEnumerable<ProductionSecretRequirement> requirements,
        string secretsDirectory)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretsDirectory);

        if (!builder.Environment.IsProduction())
        {
            return builder;
        }

        var requiredSecrets = requirements.ToArray();
        ValidateDirectory(applicationName, secretsDirectory);

        try
        {
            builder.Configuration.AddKeyPerFile(
                directoryPath: secretsDirectory,
                optional: false,
                reloadOnChange: false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Production configuration validation failed for '{applicationName}': " +
                $"a file in secrets directory '{secretsDirectory}' is not readable.",
                exception);
        }

        var secretProvider = GetSecretFileProvider(builder.Configuration, applicationName);
        foreach (var requirement in requiredSecrets)
        {
            ValidateDeliveredSecret(applicationName, secretProvider, requirement);
        }

        ProductionConfigurationPrecedenceValidator.EnsureSecretProviderWins(
            applicationName,
            builder.Configuration,
            secretProvider,
            requiredSecrets);

        builder.Services.AddHostedService(serviceProvider => new ProductionConfigurationPrecedenceValidator(
            applicationName,
            serviceProvider.GetRequiredService<IConfiguration>(),
            secretProvider,
            requiredSecrets));

        return builder;
    }

    private static void ValidateDirectory(string applicationName, string secretsDirectory)
    {
        if (!Directory.Exists(secretsDirectory))
        {
            throw new InvalidOperationException(
                $"Production configuration validation failed for '{applicationName}': " +
                $"required secrets directory '{secretsDirectory}' does not exist.");
        }

        try
        {
            _ = Directory.EnumerateFileSystemEntries(secretsDirectory).FirstOrDefault();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Production configuration validation failed for '{applicationName}': " +
                $"required secrets directory '{secretsDirectory}' is not readable.",
                exception);
        }
    }

    private static IConfigurationProvider GetSecretFileProvider(
        IConfiguration configuration,
        string applicationName)
    {
        var provider = (configuration as IConfigurationRoot)?.Providers.LastOrDefault();
        if (provider is not KeyPerFileConfigurationProvider)
        {
            throw new InvalidOperationException(
                $"Production configuration validation failed for '{applicationName}': " +
                "the KeyPerFile secret provider was not registered as the final configuration provider.");
        }

        return provider;
    }

    private static void ValidateDeliveredSecret(
        string applicationName,
        IConfigurationProvider secretProvider,
        ProductionSecretRequirement requirement)
    {
        // Only the KeyPerFile provider is consulted, so a value supplied by JSON, environment variables or
        // the command line can never satisfy a production secret-file requirement.
        if (!secretProvider.TryGet(requirement.ConfigurationKey, out var value))
        {
            throw ProductionConfigurationFailure.Create(
                applicationName,
                requirement.ConfigurationKey,
                $"required secret file '{requirement.FileName}' is missing");
        }

        var validationError = requirement.GetValidationError(value ?? string.Empty);
        if (validationError is not null)
        {
            throw ProductionConfigurationFailure.Create(applicationName, requirement.ConfigurationKey, validationError);
        }
    }
}
