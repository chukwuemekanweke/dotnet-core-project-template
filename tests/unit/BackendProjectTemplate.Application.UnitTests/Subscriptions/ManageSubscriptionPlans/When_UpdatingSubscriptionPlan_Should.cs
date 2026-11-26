using BackendProjectTemplate.Application.Subscriptions.Features.SetSubscriptionPlanActivation;
using BackendProjectTemplate.Application.Subscriptions.Features.UpdateSubscriptionPlan;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.ManageSubscriptionPlans;

public sealed class When_UpdatingSubscriptionPlan_Should
{
    [Fact]
    public async Task UpdateMutableTermsAndToggleActivation()
    {
        var currency = Currency.Create("USD", "Dollar", true);
        var plan = SubscriptionPlan.Create("BASIC", "Basic", 1000m, currency.Id, BillingInterval.Monthly, true);
        var planRepository = Substitute.For<IRepository<SubscriptionPlan>>();
        var currencyRepository = Substitute.For<IRepository<Currency>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        planRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPlan>>(), Arg.Any<CancellationToken>()).Returns(plan);
        currencyRepository.GetByIdAsync(currency.Id, Arg.Any<CancellationToken>()).Returns(currency);
        var telemetry = Substitute.For<ICustomTelemetryContext>();
        var actor = new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "c", "f");
        var auditRecorder = Substitute.For<IAdministrativeAuditRecorder>();
        var update = new UpdateSubscriptionPlanHandler(planRepository, currencyRepository, auditRecorder, telemetry, unitOfWork);
        var activation = new SetSubscriptionPlanActivationHandler(planRepository, currencyRepository, auditRecorder, telemetry, unitOfWork);

        (await update.HandleAsync(new UpdateSubscriptionPlanCommand(plan.Id, "Basic Plus", Monthly(1500m), "Basic, plus.", ["Email support"], actor), CancellationToken.None))
            .Status.ShouldBe(UpdateSubscriptionPlanStatus.Updated);
        (await update.HandleAsync(new UpdateSubscriptionPlanCommand(plan.Id, "Basic Plus", Monthly(1500m), "Basic, plus.", ["Email support"], actor), CancellationToken.None))
            .Status.ShouldBe(UpdateSubscriptionPlanStatus.Unchanged);
        (await update.HandleAsync(new UpdateSubscriptionPlanCommand(plan.Id, "", Monthly(1500m), null, [], actor), CancellationToken.None))
            .Status.ShouldBe(UpdateSubscriptionPlanStatus.Invalid);
        (await activation.HandleAsync(new SetSubscriptionPlanActivationCommand(plan.Id, false, actor), CancellationToken.None))
            .Status.ShouldBe(SetSubscriptionPlanActivationStatus.Updated);
        (await activation.HandleAsync(new SetSubscriptionPlanActivationCommand(plan.Id, false, actor), CancellationToken.None))
            .Status.ShouldBe(SetSubscriptionPlanActivationStatus.Unchanged);
        plan.Code.ShouldBe("BASIC");
        plan.GetPrice(BillingInterval.Monthly).ShouldBe(1500m);
        plan.Description.ShouldBe("Basic, plus.");
        plan.Features.ShouldBe(["Email support"]);
        plan.IsActive.ShouldBeFalse();
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
        await auditRecorder.Received(2).RecordAsync(Arg.Any<AdministrativeAuditEntry>(), Arg.Any<CancellationToken>());
        await auditRecorder.Received(1).RecordAsync(
            Arg.Is<AdministrativeAuditEntry>(entry =>
                entry.Action == AdministrativeAuditActions.SubscriptionPlans.Updated &&
                entry.Changes!.ToJson()!.Contains("\"name\":{\"previous\":\"Basic\",\"current\":\"Basic Plus\"}") &&
                entry.Changes!.ToJson()!.Contains("\"prices\":{\"previous\":{\"Monthly\":1000},\"current\":{\"Monthly\":1500}}")),
            Arg.Any<CancellationToken>());
        await auditRecorder.Received(1).RecordAsync(
            Arg.Is<AdministrativeAuditEntry>(entry => entry.Action == AdministrativeAuditActions.SubscriptionPlans.Deactivated),
            Arg.Any<CancellationToken>());

        planRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPlan>>(), Arg.Any<CancellationToken>()).Returns((SubscriptionPlan?)null);
        (await update.HandleAsync(new UpdateSubscriptionPlanCommand(plan.Id, "x", Monthly(1m), null, [], actor), CancellationToken.None))
            .Status.ShouldBe(UpdateSubscriptionPlanStatus.NotFound);
        (await activation.HandleAsync(new SetSubscriptionPlanActivationCommand(plan.Id, true, actor), CancellationToken.None))
            .Status.ShouldBe(SetSubscriptionPlanActivationStatus.NotFound);
    }

    private static Dictionary<BillingInterval, decimal> Monthly(decimal price) =>
        new() { [BillingInterval.Monthly] = price };
}
