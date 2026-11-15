namespace BackendProjectTemplate.Application.Subscriptions.Features.CreateSubscriptionPlan;

public enum CreateSubscriptionPlanStatus
{
    Created = 1,
    CodeConflict = 2,
    CurrencyNotFound = 3,
    Invalid = 4
}
