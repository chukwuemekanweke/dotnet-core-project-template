using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BackendProjectTemplate.Infrastructure.Configuration;

/// <summary>
/// Re-checks, before any hosted service or the HTTP server starts, that no configuration provider was added
/// after the production KeyPerFile provider while the rest of Program.cs ran.
/// </summary>
internal sealed class ProductionConfigurationPrecedenceValidator(
    string applicationName,
    IConfiguration configuration,
    IConfigurationProvider secretProvider,
    IReadOnlyCollection<ProductionSecretRequirement> requirements) : IHostedLifecycleService
{
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        EnsureSecretProviderWins(applicationName, configuration, secretProvider, requirements);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal static void EnsureSecretProviderWins(
        string applicationName,
        IConfiguration configuration,
        IConfigurationProvider secretProvider,
        IEnumerable<ProductionSecretRequirement> requirements)
    {
        if (configuration is not IConfigurationRoot root
            || !ReferenceEquals(root.Providers.LastOrDefault(), secretProvider))
        {
            throw new InvalidOperationException(
                $"Production configuration validation failed for '{applicationName}': " +
                "a configuration provider was added after the KeyPerFile secret provider, " +
                "so delivered secrets no longer have the highest precedence.");
        }

        foreach (var requirement in requirements)
        {
            secretProvider.TryGet(requirement.ConfigurationKey, out var deliveredValue);
            if (!string.Equals(configuration[requirement.ConfigurationKey], deliveredValue, StringComparison.Ordinal))
            {
                throw ProductionConfigurationFailure.Create(
                    applicationName,
                    requirement.ConfigurationKey,
                    "the delivered secret did not become the effective highest-precedence configuration value");
            }
        }
    }
}
