using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;

namespace BackendProjectTemplate.Application.Subscriptions.Features.ListAdminSubscriptionPlans;

public sealed class ListAdminSubscriptionPlansHandler(ISubscriptionAdministrationReadModelRepository readModelRepository)
{
    public async Task<IReadOnlyList<AdminSubscriptionPlanResponse>> HandleAsync(
        ListAdminSubscriptionPlansCommand command,
        CancellationToken cancellationToken)
    {
        var plans = await readModelRepository.ListPlansAsync(command.IsActive, cancellationToken);
        return plans.Select(AdminSubscriptionResponseMapper.ToResponse).ToArray();
    }
}
