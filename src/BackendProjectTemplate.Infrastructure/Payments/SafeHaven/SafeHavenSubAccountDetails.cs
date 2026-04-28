using System.Text.Json.Serialization;

namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenSubAccountDetails(
    [property: JsonPropertyName("_id")] string Id,
    string FirstName,
    string LastName,
    string EmailAddress,
    string Bvn);
