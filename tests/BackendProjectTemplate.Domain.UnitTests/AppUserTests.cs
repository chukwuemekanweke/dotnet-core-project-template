using BackendProjectTemplate.Domain.Authentication.Entities;
using Shouldly;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class AppUserTests
{
    [Fact]
    public void Create_SetsTrimmedNamesAndAuditFields()
    {
        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);

        var user = AppUser.Create("  ada@example.com  ", " Ada ", " Lovelace ", now);

        user.Email.ShouldBe("ada@example.com");
        user.UserName.ShouldBe("ada@example.com");
        user.FirstName.ShouldBe("Ada");
        user.LastName.ShouldBe("Lovelace");
        user.CreatedAtUtc.ShouldBe(now);
        user.UpdatedAtUtc.ShouldBe(now);
    }

    [Fact]
    public void MarkEmailVerified_UpdatesConfirmationAndTimestamp()
    {
        var createdAt = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var verifiedAt = createdAt.AddMinutes(5);
        var user = AppUser.Create("ada@example.com", "Ada", "Lovelace", createdAt);

        user.MarkEmailVerified(verifiedAt);

        user.EmailConfirmed.ShouldBeTrue();
        user.UpdatedAtUtc.ShouldBe(verifiedAt);
    }
}
