using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Entities;

public sealed class EmailProvider : Entity
{
    private EmailProvider()
    {
    }

    private EmailProvider(string providerName, string providerKey, bool isActive, DateTimeOffset utcNow)
    {
        ProviderName = providerName.Trim();
        ProviderKey = providerKey.Trim();
        IsActive = isActive;
    }

    public string ProviderName { get; private set; } = string.Empty;
    public string ProviderKey { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static EmailProvider Create(string providerName, string providerKey, bool isActive, DateTimeOffset utcNow) =>
        new(providerName, providerKey, isActive, utcNow);

    public void SetActive(bool isActive, DateTimeOffset utcNow)
    {
        IsActive = isActive;
        Touch(utcNow);
    }
}
