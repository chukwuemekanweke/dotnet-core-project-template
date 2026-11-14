using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Payments.Entities;

public sealed class BillingAccount : Entity, IAggregateRoot
{
    public const int MaxDisplayNameLength = 200;

    private BillingAccount()
    {
    }

    private BillingAccount(
        Guid tenantId,
        BillingAccountOwnerType ownerType,
        Guid? stakeholderId,
        string displayName)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (!Enum.IsDefined(ownerType))
        {
            throw new ArgumentOutOfRangeException(nameof(ownerType), ownerType, "Billing account owner type is invalid.");
        }

        if (ownerType == BillingAccountOwnerType.Tenant && stakeholderId.HasValue)
        {
            throw new ArgumentException("A tenant-owned billing account cannot have a stakeholder id.", nameof(stakeholderId));
        }

        if (ownerType == BillingAccountOwnerType.Stakeholder && (!stakeholderId.HasValue || stakeholderId.Value == Guid.Empty))
        {
            throw new ArgumentException("A stakeholder-owned billing account requires a stakeholder id.", nameof(stakeholderId));
        }

        TenantId = tenantId;
        OwnerType = ownerType;
        StakeholderId = stakeholderId;
        DisplayName = NormalizeDisplayName(displayName);
    }

    public Guid TenantId { get; private set; }
    public BillingAccountOwnerType OwnerType { get; private set; }
    public Guid? StakeholderId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;

    public static BillingAccount Create(
        Guid tenantId,
        BillingAccountOwnerType ownerType,
        Guid? stakeholderId,
        string displayName) =>
        new(tenantId, ownerType, stakeholderId, displayName);

    private static string NormalizeDisplayName(string displayName)
    {
        var normalizedDisplayName = displayName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedDisplayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (normalizedDisplayName.Length > MaxDisplayNameLength)
        {
            throw new ArgumentException(
                $"Display name must not exceed {MaxDisplayNameLength} characters.",
                nameof(displayName));
        }

        return normalizedDisplayName;
    }
}
