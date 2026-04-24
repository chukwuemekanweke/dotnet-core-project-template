using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Payments.Entities;

public sealed class PaymentProvider : Entity, IAggregateRoot
{
    private PaymentProvider()
    {
    }

    private PaymentProvider(string providerName, string providerKey, bool isActive)
    {
        ProviderName = providerName.Trim();
        ProviderKey = providerKey.Trim();
        IsActive = isActive;
    }

    public string ProviderName { get; private set; } = string.Empty;
    public string ProviderKey { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static PaymentProvider Create(
        string providerName,
        string providerKey,
        bool isActive,
        DateTimeOffset utcNow) =>
        new(providerName, providerKey, isActive);

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
