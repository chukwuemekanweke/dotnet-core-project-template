namespace BackendProjectTemplate.Infrastructure.Configuration;

internal static class ProductionConfigurationFailure
{
    // Messages name the application, key and reason only; they must never include configuration values.
    public static InvalidOperationException Create(
        string applicationName,
        string configurationKey,
        string reason) =>
        new(
            $"Production configuration validation failed for '{applicationName}': " +
            $"configuration key '{configurationKey}' is invalid because {reason}.");
}
