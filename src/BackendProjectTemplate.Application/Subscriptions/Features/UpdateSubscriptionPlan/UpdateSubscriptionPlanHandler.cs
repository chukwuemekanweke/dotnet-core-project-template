using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;

namespace BackendProjectTemplate.Application.Subscriptions.Features.UpdateSubscriptionPlan;

/// <summary>
/// Updates the mutable plan terms (name and price). The code, currency and billing interval are immutable.
/// Existing purchase, period, renewal and allocation snapshots keep their original terms.
/// </summary>
public sealed class UpdateSubscriptionPlanHandler(
    IRepository<SubscriptionPlan> planRepository,
    IRepository<Currency> currencyRepository,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<UpdateSubscriptionPlanResult> HandleAsync(
        UpdateSubscriptionPlanCommand command,
        CancellationToken cancellationToken)
    {
        var plan = await planRepository.FirstOrDefaultAsync(new TrackedSubscriptionPlanByIdSpecification(command.PlanId), cancellationToken);
        if (plan is null)
        {
            return new UpdateSubscriptionPlanResult(UpdateSubscriptionPlanStatus.NotFound, null, $"Subscription plan '{command.PlanId}' was not found.");
        }

        bool changed;
        try
        {
            changed = plan.UpdateTerms(command.Name, command.Price);
        }
        catch (ArgumentException exception)
        {
            return new UpdateSubscriptionPlanResult(UpdateSubscriptionPlanStatus.Invalid, null, exception.Message);
        }

        if (changed)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Subscriptions.PlanUpdated,
                ObservabilityEventProperties.Create(
                    command.ActorContext,
                    command.ActorContext.StakeholderId,
                    additionalProperties: new Dictionary<string, string>
                    {
                        [Observability.PropertyNames.Subscriptions.PlanId] = plan.Id.ToString()
                    }));
        }

        var currency = await currencyRepository.GetByIdAsync(plan.CurrencyId, cancellationToken)
            ?? throw new InvalidOperationException($"Currency '{plan.CurrencyId}' was not found for plan '{plan.Code}'.");
        return new UpdateSubscriptionPlanResult(
            changed ? UpdateSubscriptionPlanStatus.Updated : UpdateSubscriptionPlanStatus.Unchanged,
            AdminSubscriptionResponseMapper.ToResponse(plan, currency),
            null);
    }
}
