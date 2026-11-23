namespace BackendProjectTemplate.Domain.Subscriptions;

public static class SubscriptionPersistenceConstraints
{
    public const string EntitlementExclusionConstraintName = "EX_Subscriptions_BillingAccountEntitlement";
    public const string AllocationOperationUniqueIndexName = "UX_SubscriptionAllocations_OperationId";
    public const string PurchaseOperationUniqueIndexName = "UX_SubscriptionPurchases_TenantId_InitiatedByStakeholderId_OperationId";
    public const string PlanCodeUniqueIndexName = "IX_SubscriptionPlans_Code";
}
