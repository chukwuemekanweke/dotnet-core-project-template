using BackendProjectTemplate.Application.Subscriptions.Features.SetSubscriptionPlanActivation;
using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Auditing.AdminAuditLogs.Recording;

/// <summary>
/// An administrative mutation must not commit without its audit record. Forcing audit recording to fail
/// (an actor that cannot be resolved) must leave both the business change and every audit row uncommitted,
/// for a unit-of-work handler and for the advisory-locked user administration transaction.
/// </summary>
[Collection(nameof(ContainersCollection))]
public sealed class When_RecordingAdministrativeAudit_WithUnresolvableActor_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    [Fact]
    public async Task RollBackTheMutationAndItsAuditRecords()
    {
        var unknownStakeholder = new ActorContext(Guid.CreateVersion7(), AdminTenantId, "audit-failure", "audit-failure");
        using (var scope = CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<SetSubscriptionPlanActivationHandler>();
            await Should.ThrowAsync<InvalidOperationException>(() => handler.HandleAsync(
                new SetSubscriptionPlanActivationCommand(PlanId, false, unknownStakeholder),
                CancellationToken.None));
        }

        // The authenticated stakeholder belongs to a different user than the claimed actor.
        Guid targetUserId;
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            targetUserId = await dbContext.Stakeholders
                .Where(stakeholder => stakeholder.Id == TargetStakeholderId)
                .Select(stakeholder => stakeholder.AppUserId)
                .SingleAsync();
            var administration = scope.ServiceProvider.GetRequiredService<IAdminUserAccountAdministration>();
            await Should.ThrowAsync<InvalidOperationException>(() => administration.LockAsync(
                new AdminUserAccountRequest(
                    AdminUserId,
                    targetUserId,
                    "Mismatched actor",
                    new ActorContext(ForeignStakeholderId, AdminTenantId, "audit-mismatch", "audit-mismatch")),
                CancellationToken.None));
        }

        using var verificationScope = CreateScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verification.SubscriptionPlans.SingleAsync(plan => plan.Id == PlanId)).IsActive.ShouldBeTrue();
        (await verification.Users.SingleAsync(user => user.Id == targetUserId)).IsAdministrativelyLocked.ShouldBeFalse();
        (await verification.AdministrativeSecurityAudits.AnyAsync(audit => audit.TargetUserId == targetUserId)).ShouldBeFalse();
        (await verification.AdministrativeAudits.AnyAsync(audit =>
            audit.CorrelationId == "audit-failure" || audit.CorrelationId == "audit-mismatch")).ShouldBeFalse();
    }
}
