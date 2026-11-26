using BackendProjectTemplate.Domain.Auditing;

namespace BackendProjectTemplate.Domain.UnitTests.Auditing;

public sealed class When_CreatingAdministrativeAudit_WithInvalidIdentity_Should
{
    [Fact]
    public void RejectMissingActorTargetAndUncataloguedIdentifiers()
    {
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(
            actor: new AdministrativeAuditActor(Guid.Empty, null, "Name")));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(
            actor: new AdministrativeAuditActor(Guid.CreateVersion7(), Guid.Empty, "Name")));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(
            actor: new AdministrativeAuditActor(Guid.CreateVersion7(), null, "  ")));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(targetId: Guid.Empty));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(action: "users.lockd"));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(action: "USERS.LOCKED"));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(targetType: "Invoice"));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(correlationId: " "));
    }
}
