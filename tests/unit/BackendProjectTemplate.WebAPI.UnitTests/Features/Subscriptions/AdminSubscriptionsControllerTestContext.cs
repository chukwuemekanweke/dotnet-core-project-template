using BackendProjectTemplate.Application.Subscriptions.Features.AdministrativelyCancelSubscriptionAtPeriodEnd;
using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionImmediately;
using BackendProjectTemplate.Application.Subscriptions.Features.CreateSubscriptionPlan;
using BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;
using BackendProjectTemplate.Application.Subscriptions.Features.ListAdminSubscriptionPlans;
using BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptions;
using BackendProjectTemplate.Application.Subscriptions.Features.RetrySubscriptionPurchaseFulfillment;
using BackendProjectTemplate.Application.Subscriptions.Features.SetSubscriptionPlanActivation;
using BackendProjectTemplate.Application.Subscriptions.Features.UpdateSubscriptionPlan;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions;

internal sealed class AdminSubscriptionsControllerTestContext
{
    public AdminSubscriptionsControllerTestContext()
    {
        CurrentActor.ActorId.Returns(Guid.CreateVersion7().ToString());
        CurrentActor.CorrelationId.Returns("correlation");
        CurrentActor.FlowId.Returns("flow");
    }

    public ICurrentActor CurrentActor { get; } = Substitute.For<ICurrentActor>();
    public IRepository<SubscriptionPlan> PlanRepository { get; } = Substitute.For<IRepository<SubscriptionPlan>>();
    public IRepository<SubscriptionAggregate> SubscriptionRepository { get; } = Substitute.For<IRepository<SubscriptionAggregate>>();
    public IRepository<SubscriptionAllocation> AllocationRepository { get; } = Substitute.For<IRepository<SubscriptionAllocation>>();
    public IRepository<Currency> CurrencyRepository { get; } = Substitute.For<IRepository<Currency>>();
    public IBillingAccountResolver BillingAccountResolver { get; } = Substitute.For<IBillingAccountResolver>();
    public ISubscriptionAdministrationReadModelRepository ReadModels { get; } = Substitute.For<ISubscriptionAdministrationReadModelRepository>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IAdministrativeAuditRecorder AuditRecorder { get; } = Substitute.For<IAdministrativeAuditRecorder>();

    public AdminSubscriptionsController CreateSubscriptionsController()
    {
        var telemetry = Substitute.For<ICustomTelemetryContext>();
        var commandSender = Substitute.For<ICommandSender>();
        var lifecycle = Substitute.For<ISubscriptionLifecyclePersistence>();
        var controller = new AdminSubscriptionsController(
            new ListSubscriptionsValidator(),
            new ListSubscriptionsHandler(ReadModels),
            new GetSubscriptionDetailValidator(),
            new GetSubscriptionDetailHandler(ReadModels),
            new AllocateSubscriptionValidator(),
            new AllocateSubscriptionHandler(
                PlanRepository,
                SubscriptionRepository,
                AllocationRepository,
                BillingAccountResolver,
                commandSender,
                AuditRecorder,
                telemetry,
                UnitOfWork,
                TimeProvider.System,
                Substitute.For<ILogger<AllocateSubscriptionHandler>>()),
            new AdminSubscriptionCancellationValidator(),
            new CancelSubscriptionImmediatelyHandler(
                SubscriptionRepository,
                Substitute.For<IRepository<SubscriptionRenewal>>(),
                lifecycle,
                AuditRecorder,
                telemetry,
                UnitOfWork,
                TimeProvider.System,
                Substitute.For<ILogger<CancelSubscriptionImmediatelyHandler>>()),
            new AdministrativelyCancelSubscriptionAtPeriodEndHandler(SubscriptionRepository, lifecycle, commandSender, AuditRecorder, telemetry, UnitOfWork, TimeProvider.System),
            new RetryPurchaseFulfillmentValidator(),
            new RetrySubscriptionPurchaseFulfillmentHandler(
                Substitute.For<IRepository<SubscriptionPurchase>>(),
                Substitute.For<IRepository<PaymentTransaction>>(),
                commandSender,
                AuditRecorder,
                telemetry,
                UnitOfWork),
            CurrentActor);
        controller.ProblemDetailsFactory = new TestProblemDetailsFactory();
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    public AdminSubscriptionPlansController CreatePlansController()
    {
        var telemetry = Substitute.For<ICustomTelemetryContext>();
        var controller = new AdminSubscriptionPlansController(
            new ListAdminSubscriptionPlansHandler(ReadModels),
            new CreateSubscriptionPlanValidator(),
            new CreateSubscriptionPlanHandler(PlanRepository, CurrencyRepository, AuditRecorder, telemetry, UnitOfWork),
            new UpdateSubscriptionPlanValidator(),
            new UpdateSubscriptionPlanHandler(PlanRepository, CurrencyRepository, AuditRecorder, telemetry, UnitOfWork),
            new SetSubscriptionPlanActivationValidator(),
            new SetSubscriptionPlanActivationHandler(PlanRepository, CurrencyRepository, AuditRecorder, telemetry, UnitOfWork),
            CurrentActor);
        controller.ProblemDetailsFactory = new TestProblemDetailsFactory();
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    public SubscriptionPlan ReturnActivePlan()
    {
        var plan = SubscriptionPlan.Create("ADMIN_TEST", "Admin Test", 1000m, Guid.CreateVersion7(), BillingInterval.Monthly, true);
        PlanRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPlan>>(), Arg.Any<CancellationToken>()).Returns(plan);
        return plan;
    }

    public static AllocateSubscriptionRequest CreateAllocationRequest(Guid planId, bool? autoRenew = null) =>
        new(Guid.CreateVersion7(), "Tenant", null, planId, null, null, "Promotion", "Launch promotion", Guid.CreateVersion7(), autoRenew);

    private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new() { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new(modelStateDictionary) { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };
    }
}
