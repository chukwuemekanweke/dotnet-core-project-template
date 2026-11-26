using BackendProjectTemplate.Domain.Auditing;

namespace BackendProjectTemplate.Domain.UnitTests.Auditing;

public sealed class When_CreatingAdministrativeAudit_WithValidInput_Should
{
    [Fact]
    public void CaptureNormalizedSnapshotsAndChanges()
    {
        var targetId = Guid.CreateVersion7();
        var actorTenantId = Guid.CreateVersion7();
        var changes = new AdministrativeAuditChanges().Field("isAdministrativelyLocked", false, true).ToJson();

        var audit = AdministrativeAuditTestData.Create(
            targetId: targetId,
            targetDisplayName: "  Tunde Bakare ",
            reason: " Suspected account takeover ",
            changes: changes,
            actorTenantId: actorTenantId);

        audit.ActorUserId.ShouldBe(AdministrativeAuditTestData.Actor.UserId);
        audit.ActorStakeholderId.ShouldBe(AdministrativeAuditTestData.Actor.StakeholderId);
        audit.ActorDisplayName.ShouldBe("Chidi Nwosu");
        audit.Action.ShouldBe(AdministrativeAuditActions.Users.Locked);
        audit.TargetType.ShouldBe(AdministrativeAuditTargetTypes.User);
        audit.TargetId.ShouldBe(targetId);
        audit.TargetDisplayName.ShouldBe("Tunde Bakare");
        audit.Reason.ShouldBe("Suspected account takeover");
        audit.ActorTenantId.ShouldBe(actorTenantId);
        audit.TargetTenantId.ShouldBeNull();
        audit.CorrelationId.ShouldBe("request-correlation-id");
        audit.FlowId.ShouldBe("flow-id");
        audit.Changes.ShouldBe("{\"isAdministrativelyLocked\":{\"previous\":false,\"current\":true}}");
        audit.OccurredAtUtc.ShouldBe(AdministrativeAuditTestData.OccurredAtUtc);
        audit.Id.ShouldNotBe(Guid.Empty);
    }
}
