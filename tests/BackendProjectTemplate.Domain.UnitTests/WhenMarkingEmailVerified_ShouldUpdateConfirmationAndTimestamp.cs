using BackendProjectTemplate.Domain.Authentication.Entities;
using Shouldly;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenMarkingEmailVerified_ShouldUpdateConfirmationAndTimestamp
{
    [Fact]
    public void Verify()
    {
        const string email = "ada@example.com";
        const string firstName = "Ada";
        const string lastName = "Lovelace";

        var createdAt = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var verifiedAt = createdAt.AddMinutes(5);
        var user = AppUser.Create(email, firstName, lastName, createdAt);

        user.MarkEmailVerified(verifiedAt);

        user.EmailConfirmed.ShouldBeTrue();
        user.UpdatedAtUtc.ShouldBe(verifiedAt);
    }
}
