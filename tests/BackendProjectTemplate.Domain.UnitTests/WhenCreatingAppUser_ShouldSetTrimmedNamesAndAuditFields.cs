using BackendProjectTemplate.Domain.Authentication.Entities;
using Shouldly;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenCreatingAppUser_ShouldSetTrimmedNamesAndAuditFields
{
    [Fact]
    public void Verify()
    {
        const string rawEmail = "  ada@example.com  ";
        const string rawFirstName = " Ada ";
        const string rawLastName = " Lovelace ";
        const string expectedEmail = "ada@example.com";
        const string expectedFirstName = "Ada";
        const string expectedLastName = "Lovelace";

        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);

        var user = AppUser.Create(rawEmail, rawFirstName, rawLastName, now);

        user.Email.ShouldBe(expectedEmail);
        user.UserName.ShouldBe(expectedEmail);
        user.FirstName.ShouldBe(expectedFirstName);
        user.LastName.ShouldBe(expectedLastName);
        user.CreatedAtUtc.ShouldBe(now);
        user.UpdatedAtUtc.ShouldBe(now);
    }
}
