using BackendProjectTemplate.Application.Subscriptions.Features.SetSubscriptionPlanActivation;
using BackendProjectTemplate.Application.Subscriptions.Features.UpdateSubscriptionPlan;
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
        var update = new UpdateSubscriptionPlanHandler(planRepository, currencyRepository, telemetry, unitOfWork);
        var activation = new SetSubscriptionPlanActivationHandler(planRepository, currencyRepository, telemetry, unitOfWork);

        (await update.HandleAsync(new UpdateSubscriptionPlanCommand(plan.Id, "Basic Plus", 1500m, actor), CancellationToken.None))
            .Status.ShouldBe(UpdateSubscriptionPlanStatus.Updated);
        (await update.HandleAsync(new UpdateSubscriptionPlanCommand(plan.Id, "Basic Plus", 1500m, actor), CancellationToken.None))
            .Status.ShouldBe(UpdateSubscriptionPlanStatus.Unchanged);
        (await update.HandleAsync(new UpdateSubscriptionPlanCommand(plan.Id, "", 1500m, actor), CancellationToken.None))
            .Status.ShouldBe(UpdateSubscriptionPlanStatus.Invalid);
        (await activation.HandleAsync(new SetSubscriptionPlanActivationCommand(plan.Id, false, actor), CancellationToken.None))
            .Status.ShouldBe(SetSubscriptionPlanActivationStatus.Updated);
        (await activation.HandleAsync(new SetSubscriptionPlanActivationCommand(plan.Id, false, actor), CancellationToken.None))
            .Status.ShouldBe(SetSubscriptionPlanActivationStatus.Unchanged);
        plan.Code.ShouldBe("BASIC");
        plan.Price.ShouldBe(1500m);
        plan.IsActive.ShouldBeFalse();
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());

        planRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPlan>>(), Arg.Any<CancellationToken>()).Returns((SubscriptionPlan?)null);
        (await update.HandleAsync(new UpdateSubscriptionPlanCommand(plan.Id, "x", 1m, actor), CancellationToken.None))
            .Status.ShouldBe(UpdateSubscriptionPlanStatus.NotFound);
        (await activation.HandleAsync(new SetSubscriptionPlanActivationCommand(plan.Id, true, actor), CancellationToken.None))
            .Status.ShouldBe(SetSubscriptionPlanActivationStatus.NotFound);
    }
}
