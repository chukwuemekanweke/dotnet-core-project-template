using BackendProjectTemplate.Domain.Authentication.Entities;
using Shouldly;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenCreatingAppUser_Should
{
    [Fact]
    public void SetTrimmedNamesAndAuditFields()
    {
        const string rawEmail = "  ada@example.com  ";
        const string expectedEmail = "ada@example.com";

        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);

        var user = AppUser.Create(rawEmail, now);

        user.Email.ShouldBe(expectedEmail);
        user.UserName.ShouldBe(expectedEmail);
        user.CreatedAtUtc.ShouldBe(default);
        user.UpdatedAtUtc.ShouldBe(default);
    }
}

