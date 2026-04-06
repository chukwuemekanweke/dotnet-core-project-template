using Bogus;

namespace BackendProjectTemplate.Consumer.UnitTests;

internal static class ConsumerTestData
{
    private static readonly Faker Faker = new();

    public static string Email() => Faker.Internet.Email().ToLowerInvariant();

    public static string FirstName() => Faker.Name.FirstName();

    public static string LastName() => Faker.Name.LastName();

    public static string Otp() => Faker.Random.ReplaceNumbers("######");
}
