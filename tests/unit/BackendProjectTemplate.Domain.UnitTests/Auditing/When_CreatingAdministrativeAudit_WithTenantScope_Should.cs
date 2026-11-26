using BackendProjectTemplate.Domain.Auditing;

namespace BackendProjectTemplate.Domain.UnitTests.Auditing;

public sealed class When_CreatingAdministrativeAudit_WithTenantScope_Should
{
    [Fact]
    public void RequireTheTargetTenantOnlyForTenantScopedTargets()
    {
        var actorTenantId = Guid.CreateVersion7();
        var targetTenantId = Guid.CreateVersion7();

        var subscription = AdministrativeAuditTestData.Create(
            action: AdministrativeAuditActions.Subscriptions.Allocated,
            targetType: AdministrativeAuditTargetTypes.Subscription,
            targetTenantId: targetTenantId,
            actorTenantId: actorTenantId);
        subscription.TargetTenantId.ShouldBe(targetTenantId);
        subscription.ActorTenantId.ShouldBe(actorTenantId);

        // An unresolved tenant never silently becomes a global record.
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(
            action: AdministrativeAuditActions.Subscriptions.Allocated,
            targetType: AdministrativeAuditTargetTypes.Subscription,
            targetTenantId: null,
            actorTenantId: actorTenantId));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(
            action: AdministrativeAuditActions.Users.SessionRevoked,
            targetType: AdministrativeAuditTargetTypes.AuthenticationSession,
            targetTenantId: null));

        // Global targets are deliberately tenant-less.
        AdministrativeAuditTestData.Create(
            action: AdministrativeAuditActions.Providers.Activated,
            targetType: AdministrativeAuditTargetTypes.Provider).TargetTenantId.ShouldBeNull();
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(
            action: AdministrativeAuditActions.SubscriptionPlans.Updated,
            targetType: AdministrativeAuditTargetTypes.SubscriptionPlan,
            targetTenantId: targetTenantId));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(targetTenantId: Guid.Empty));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(actorTenantId: Guid.Empty));
    }
}
