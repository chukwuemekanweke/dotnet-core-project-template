using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenCreatingEmailProvider_ShouldSetProviderNameProviderKeyIsActiveAndAuditFields
{
    [Fact]
    public void Verify()
    {
        var utcNow = DateTimeOffset.UtcNow;

        var provider = Provider.Create(ProviderType.Email, " Mailtrap ", " mailtrap ", true, utcNow);

        provider.ProviderName.ShouldBe("Mailtrap");
        provider.ProviderKey.ShouldBe("mailtrap");
        provider.IsActive.ShouldBeTrue();
        provider.CreatedAtUtc.ShouldBe(default);
        provider.UpdatedAtUtc.ShouldBe(default);
    }
}

