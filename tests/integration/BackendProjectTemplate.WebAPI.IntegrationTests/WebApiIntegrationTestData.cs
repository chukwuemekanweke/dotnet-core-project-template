using Bogus;

namespace BackendProjectTemplate.WebAPI.IntegrationTests;

internal static class WebApiIntegrationTestData
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
