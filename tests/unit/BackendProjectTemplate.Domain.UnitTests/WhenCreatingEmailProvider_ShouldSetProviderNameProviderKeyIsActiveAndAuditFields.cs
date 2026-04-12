using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenCreatingEmailProvider_ShouldSetProviderNameProviderKeyIsActiveAndAuditFields
{
    [Fact]
    public void Verify()
    {
        var utcNow = DateTimeOffset.UtcNow;

        var emailProvider = EmailProvider.Create(" Mailtrap ", " mailtrap ", true, utcNow);

        emailProvider.ProviderName.ShouldBe("Mailtrap");
        emailProvider.ProviderKey.ShouldBe("mailtrap");
        emailProvider.IsActive.ShouldBeTrue();
        emailProvider.CreatedAtUtc.ShouldBe(default);
        emailProvider.UpdatedAtUtc.ShouldBe(default);
    }
}
