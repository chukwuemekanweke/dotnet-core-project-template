using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Entities;

public enum ProviderType
{
    Email = 1,
    FileStorage = 2
}

public sealed class Provider : Entity, IAggregateRoot
{
    private Provider()
    {
    }

    private Provider(ProviderType providerType, string providerName, string providerKey, bool isActive, DateTimeOffset utcNow)
    {
        ProviderType = providerType;
        ProviderName = providerName.Trim();
        ProviderKey = providerKey.Trim();
        IsActive = isActive;
    }

    public ProviderType ProviderType { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string ProviderKey { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static Provider Create(
        ProviderType providerType,
        string providerName,
        string providerKey,
        bool isActive,
        DateTimeOffset utcNow) =>
        new(providerType, providerName, providerKey, isActive, utcNow);

    public void SetActive(bool isActive, DateTimeOffset utcNow)
    {
        IsActive = isActive;
        Touch(utcNow);
    }
}
