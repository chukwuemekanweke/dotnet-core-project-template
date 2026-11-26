using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;

namespace BackendProjectTemplate.Application.Subscriptions.Features.SetSubscriptionPlanActivation;

/// <summary>
/// Activates or deactivates a plan. Deactivation stops new purchases and allocations only; already granted
/// subscriptions and scheduled allocations keep their committed terms and are never terminated by it.
/// </summary>
public sealed class SetSubscriptionPlanActivationHandler(
    IRepository<SubscriptionPlan> planRepository,
    IRepository<Currency> currencyRepository,
    IAdministrativeAuditRecorder auditRecorder,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<SetSubscriptionPlanActivationResult> HandleAsync(
        SetSubscriptionPlanActivationCommand command,
        CancellationToken cancellationToken)
    {
        var plan = await planRepository.FirstOrDefaultAsync(new TrackedSubscriptionPlanByIdSpecification(command.PlanId), cancellationToken);
        if (plan is null)
        {
            return new SetSubscriptionPlanActivationResult(
                SetSubscriptionPlanActivationStatus.NotFound,
                null,
                $"Subscription plan '{command.PlanId}' was not found.");
        }

        var changed = plan.IsActive != command.IsActive;
        if (changed)
        {
            plan.SetActive(command.IsActive);
            await auditRecorder.RecordAsync(
                new AdministrativeAuditEntry(
                    command.ActorContext,
                    command.IsActive
                        ? AdministrativeAuditActions.SubscriptionPlans.Activated
                        : AdministrativeAuditActions.SubscriptionPlans.Deactivated,
                    AdministrativeAuditTargetTypes.SubscriptionPlan,
                    plan.Id,
                    plan.Name,
                    null,
                    command.Reason,
                    new AdministrativeAuditChanges().Field("isActive", !command.IsActive, command.IsActive)),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Subscriptions.PlanActivationChanged,
                ObservabilityEventProperties.Create(
                    command.ActorContext,
                    command.ActorContext.StakeholderId,
                    additionalProperties: new Dictionary<string, string>
                    {
                        [Observability.PropertyNames.Subscriptions.PlanId] = plan.Id.ToString(),
                        [Observability.PropertyNames.Subscriptions.Status] = command.IsActive ? "active" : "inactive"
                    }));
        }

        var currency = await currencyRepository.GetByIdAsync(plan.CurrencyId, cancellationToken)
            ?? throw new InvalidOperationException($"Currency '{plan.CurrencyId}' was not found for plan '{plan.Code}'.");
        return new SetSubscriptionPlanActivationResult(
            changed ? SetSubscriptionPlanActivationStatus.Updated : SetSubscriptionPlanActivationStatus.Unchanged,
            AdminSubscriptionResponseMapper.ToResponse(plan, currency),
            null);
    }
}
