using Bogus;

namespace BackendProjectTemplate.Application.UnitTests.Authentication;

internal static class AuthenticationTestData
{
    private static readonly Faker Faker = new();

    public static string Email(string? prefix = null)
    {
        var localPart = $"{prefix ?? Faker.Internet.UserName()}-{Faker.Random.Guid():N}"
            .Replace(".", "-", StringComparison.Ordinal)
            .ToLowerInvariant();

        return $"{localPart}@example.com";
    }

    public static string FirstName() => Faker.Name.FirstName();

    public static string LastName() => Faker.Name.LastName();

    public static string StrongPassword() => $"Aa1!{Faker.Random.Replace("????????")}";

    public static string WeakPassword() => Faker.Random.Replace("????????");

    public static string Otp() => Faker.Random.ReplaceNumbers("######");
}
