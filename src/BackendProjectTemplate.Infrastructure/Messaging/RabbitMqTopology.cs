namespace BackendProjectTemplate.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ names scoped by the hosting environment (DOTNET_ENVIRONMENT / ASPNETCORE_ENVIRONMENT). Queue
/// names are scoped to the virtual host, not the exchange, so every exchange, queue and subscription carries
/// the environment segment; hosts of the same application in different environments never become competing
/// consumers on a shared broker, even with the same connection settings.
/// </summary>
public sealed record RabbitMqTopology(
    string Environment,
    string EventsExchange,
    string CommandsExchange,
    string EventsSubscriptionName,
    string CommandsQueueName)
{
    public static RabbitMqTopology Create(RabbitMqMessagingOptions options, string hostEnvironmentName)
    {
        ArgumentNullException.ThrowIfNull(options);

        var environment = NormalizeEnvironment(hostEnvironmentName);

        return new RabbitMqTopology(
            environment,
            $"{options.EventsExchange}.{environment}",
            $"{options.CommandsExchange}.{environment}",
            $"{options.ServiceName}.{environment}.events",
            $"{options.ServiceName}.{environment}.commands");
    }

    private static string NormalizeEnvironment(string? environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new InvalidOperationException("A hosting environment name is required to scope RabbitMQ names.");
        }

        var normalized = environment.Trim().ToLowerInvariant();
        if (!IsValidSegment(normalized))
        {
            throw new InvalidOperationException(
                $"Hosting environment '{environment}' must contain only letters, digits and hyphens to scope RabbitMQ names.");
        }

        return normalized;
    }

    private static bool IsValidSegment(string value) =>
        value[0] != '-' &&
        value[^1] != '-' &&
        !value.Contains("--", StringComparison.Ordinal) &&
        value.All(character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-');
}
