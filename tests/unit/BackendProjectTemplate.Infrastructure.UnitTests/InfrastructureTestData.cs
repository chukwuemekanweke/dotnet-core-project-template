using Bogus;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

internal static class InfrastructureTestData
{
    private static readonly Faker Faker = new();

    public static string Email()
    {
        var localPart = $"{Faker.Internet.UserName()}-{Faker.Random.Guid():N}"
            .Replace(".", "-", StringComparison.Ordinal)
            .ToLowerInvariant();

        return $"{localPart}@example.com";
    }

    public static string FirstName() => Faker.Name.FirstName();

    public static string LastName() => Faker.Name.LastName();
}
