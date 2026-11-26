using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Auditing.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Auditing;

public sealed class When_CreatingAdministrativeAudit_WithBoundaryValues_Should
{
    [Fact]
    public void EnforceLengthsUtcAndOptionalFields()
    {
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(
            reason: new string('r', AdministrativeAudit.MaxReasonLength + 1)));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(
            targetDisplayName: new string('n', AdministrativeAudit.MaxDisplayNameLength + 1)));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(
            actor: new AdministrativeAuditActor(Guid.CreateVersion7(), null, new string('a', AdministrativeAudit.MaxDisplayNameLength + 1))));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(
            correlationId: new string('c', AdministrativeAudit.MaxCorrelationLength + 1)));
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(
            occurredAtUtc: new DateTimeOffset(2026, 11, 24, 17, 2, 0, TimeSpan.FromHours(1))));
        var oversizedValue = new string('x', AdministrativeAudit.MaxChangesLength);
        Should.Throw<ArgumentException>(() => AdministrativeAuditTestData.Create(
            changes: $"{{\"name\":{{\"previous\":null,\"current\":\"{oversizedValue}\"}}}}"));

        AdministrativeAuditTestData.Create(reason: new string('r', AdministrativeAudit.MaxReasonLength)).Reason!.Length
            .ShouldBe(AdministrativeAudit.MaxReasonLength);

        // The flow identifier is a client-supplied tracing hint, so it is truncated rather than rejected.
        AdministrativeAuditTestData.Create(flowId: new string('f', 300)).FlowId!.Length.ShouldBe(AdministrativeAudit.MaxCorrelationLength);

        var minimal = AdministrativeAuditTestData.Create(
            actor: new AdministrativeAuditActor(Guid.CreateVersion7(), null, AdministrativeAuditActor.FallbackDisplayName),
            targetDisplayName: " ",
            reason: null,
            flowId: null);
        minimal.ActorStakeholderId.ShouldBeNull();
        minimal.TargetDisplayName.ShouldBeNull();
        minimal.Reason.ShouldBeNull();
        minimal.FlowId.ShouldBeNull();
        minimal.Changes.ShouldBeNull();
    }
}
