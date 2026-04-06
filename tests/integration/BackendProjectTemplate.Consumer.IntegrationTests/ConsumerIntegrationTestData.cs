using Bogus;

namespace BackendProjectTemplate.Consumer.IntegrationTests;

internal static class ConsumerIntegrationTestData
{
    private static readonly Faker Faker = new();

    public static string Email(string prefix)
    {
        var localPart = $"{prefix}-{Faker.Internet.UserName()}-{Faker.Random.Guid():N}"
            .Replace(".", "-", StringComparison.Ordinal)
            .ToLowerInvariant();

        return $"{localPart}@example.com";
    }

    public static string FirstName() => Faker.Name.FirstName();

    public static string LastName() => Faker.Name.LastName();
}
