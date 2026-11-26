using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Specifications;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;

namespace BackendProjectTemplate.Application.Subscriptions.Features.CreateSubscriptionPlan;

public sealed class CreateSubscriptionPlanHandler(
    IRepository<SubscriptionPlan> planRepository,
    IRepository<Currency> currencyRepository,
    IAdministrativeAuditRecorder auditRecorder,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<CreateSubscriptionPlanResult> HandleAsync(
        CreateSubscriptionPlanCommand command,
        CancellationToken cancellationToken)
    {
        SubscriptionPlan plan;
        try
        {
            plan = SubscriptionPlan.Create(
                command.Code,
                command.Name,
                command.CurrencyId,
                command.Prices,
                command.IsActive,
                command.Description,
                command.Features);
        }
        catch (ArgumentException exception)
        {
            return new CreateSubscriptionPlanResult(CreateSubscriptionPlanStatus.Invalid, null, exception.Message);
        }

        var currency = await currencyRepository.FirstOrDefaultAsync(
            new ActiveCurrencyByIdSpecification(command.CurrencyId),
            cancellationToken);
        if (currency is null)
        {
            return new CreateSubscriptionPlanResult(
                CreateSubscriptionPlanStatus.CurrencyNotFound,
                null,
                $"Currency '{command.CurrencyId}' was not found or is inactive.");
        }

        if (await planRepository.AnyAsync(new SubscriptionPlanByCodeSpecification(plan.Code), cancellationToken))
        {
            return CodeConflict(plan.Code);
        }

        await planRepository.AddAsync(plan, cancellationToken);
        await auditRecorder.RecordAsync(
            new AdministrativeAuditEntry(
                command.ActorContext,
                AdministrativeAuditActions.SubscriptionPlans.Created,
                AdministrativeAuditTargetTypes.SubscriptionPlan,
                plan.Id,
                plan.Name,
                null,
                command.Reason,
                new AdministrativeAuditChanges()
                    .Field("planCode", (string?)null, plan.Code)
                    .Field("name", (string?)null, plan.Name)
                    .Field("currencyCode", (string?)null, currency.CurrencyCode)
                    .Field("prices", (SortedDictionary<string, decimal>?)null, SubscriptionPlanAuditFields.Prices(plan))
                    .Field("isActive", (bool?)null, plan.IsActive)
                    .Field("description", (string?)null, plan.Description)
                    .Field("features", (string[]?)null, plan.Features.Count == 0 ? null : SubscriptionPlanAuditFields.Features(plan))),
            cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (SubscriptionPlanCodeConflictException)
        {
            return CodeConflict(plan.Code);
        }

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Subscriptions.PlanCreated,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                command.ActorContext.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Subscriptions.PlanId] = plan.Id.ToString()
                }));
        return new CreateSubscriptionPlanResult(
            CreateSubscriptionPlanStatus.Created,
            AdminSubscriptionResponseMapper.ToResponse(plan, currency),
            null);
    }

    private static CreateSubscriptionPlanResult CodeConflict(string code) =>
        new(CreateSubscriptionPlanStatus.CodeConflict, null, $"A subscription plan with code '{code}' already exists.");
}
