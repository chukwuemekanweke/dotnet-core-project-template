using BackendProjectTemplate.Application.Subscriptions.Features.CreateSubscriptionPlan;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.ManageSubscriptionPlans;

public sealed class When_CreatingSubscriptionPlan_WithValidTerms_Should
{
    [Fact]
    public async Task NormalizeCodeAndRejectDuplicatesOrUnknownCurrency()
    {
        var planRepository = Substitute.For<IRepository<SubscriptionPlan>>();
        var currencyRepository = Substitute.For<IRepository<Currency>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currency = Currency.Create("NGN", "Naira", true);
        currencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Currency>>(), Arg.Any<CancellationToken>()).Returns(currency);
        var auditRecorder = Substitute.For<IAdministrativeAuditRecorder>();
        var handler = new CreateSubscriptionPlanHandler(planRepository, currencyRepository, auditRecorder, Substitute.For<ICustomTelemetryContext>(), unitOfWork);
        var actor = new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "c", "f");
        var command = new CreateSubscriptionPlanCommand(
            " gold ",
            "Gold",
            new Dictionary<BillingInterval, decimal> { [BillingInterval.Monthly] = 120000m, [BillingInterval.Yearly] = 1200000m },
            currency.Id,
            true,
            "For larger teams.",
            ["Unlimited team members"],
            actor);

        var created = await handler.HandleAsync(command, CancellationToken.None);
        planRepository.AnyAsync(Arg.Any<ISpecification<SubscriptionPlan>>(), Arg.Any<CancellationToken>()).Returns(true);
        var duplicate = await handler.HandleAsync(command, CancellationToken.None);
        planRepository.AnyAsync(Arg.Any<ISpecification<SubscriptionPlan>>(), Arg.Any<CancellationToken>()).Returns(false);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns<int>(_ => throw new SubscriptionPlanCodeConflictException("race"));
        var raced = await handler.HandleAsync(command, CancellationToken.None);
        currencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Currency>>(), Arg.Any<CancellationToken>()).Returns((Currency?)null);
        var unknownCurrency = await handler.HandleAsync(command, CancellationToken.None);
        var invalid = await handler.HandleAsync(command with { Prices = new Dictionary<BillingInterval, decimal> { [BillingInterval.Monthly] = 10.5m } }, CancellationToken.None);

        created.Status.ShouldBe(CreateSubscriptionPlanStatus.Created);
        created.Plan!.Code.ShouldBe("GOLD");
        created.Plan.CurrencyCode.ShouldBe("NGN");
        created.Plan.Prices.Select(price => (price.BillingInterval, price.Price))
            .ShouldBe([(nameof(BillingInterval.Monthly), 120000m), (nameof(BillingInterval.Yearly), 1200000m)]);
        created.Plan.Description.ShouldBe("For larger teams.");
        created.Plan.Features.ShouldBe(["Unlimited team members"]);
        duplicate.Status.ShouldBe(CreateSubscriptionPlanStatus.CodeConflict);
        raced.Status.ShouldBe(CreateSubscriptionPlanStatus.CodeConflict);
        unknownCurrency.Status.ShouldBe(CreateSubscriptionPlanStatus.CurrencyNotFound);
        invalid.Status.ShouldBe(CreateSubscriptionPlanStatus.Invalid);
        // Only the created plan and the raced attempt staged a record; the raced save failed, so it never committed.
        await auditRecorder.Received(2).RecordAsync(
            Arg.Is<AdministrativeAuditEntry>(entry =>
                entry.Action == AdministrativeAuditActions.SubscriptionPlans.Created &&
                entry.TargetType == AdministrativeAuditTargetTypes.SubscriptionPlan &&
                entry.TargetTenantId == null),
            Arg.Any<CancellationToken>());
    }
}
