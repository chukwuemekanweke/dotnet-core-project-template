namespace BackendProjectTemplate.Domain.Common.Authentication;

public static class StepUpMfaOperations
{
    public const string AllocateSubscription = "admin.subscriptions.allocate";
    public const string CancelSubscriptionImmediately = "admin.subscriptions.cancel-immediately";
    public const string CancelSubscriptionAtPeriodEnd = "admin.subscriptions.cancel-at-period-end";
    public const string RetrySubscriptionPurchaseFulfillment = "admin.subscriptions.retry-purchase-fulfillment";
    public const string CreateSubscriptionPlan = "admin.subscription-plans.create";
    public const string UpdateSubscriptionPlan = "admin.subscription-plans.update";
    public const string SetSubscriptionPlanActivation = "admin.subscription-plans.set-activation";
    public const string SetPaymentProviderActivation = "admin.payment-providers.set-activation";
    public const string ActivateProvider = "admin.providers.activate";
    public const string LockUser = "admin.users.lock";
    public const string UnlockUser = "admin.users.unlock";
    public const string RevokeUserSession = "admin.users.sessions.revoke";
    public const string RevokeAllUserSessions = "admin.users.sessions.revoke-all";
    public const string AssignUserRole = "admin.users.roles.assign";
    public const string RemoveUserRole = "admin.users.roles.remove";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        AllocateSubscription,
        CancelSubscriptionImmediately,
        CancelSubscriptionAtPeriodEnd,
        RetrySubscriptionPurchaseFulfillment,
        CreateSubscriptionPlan,
        UpdateSubscriptionPlan,
        SetSubscriptionPlanActivation,
        SetPaymentProviderActivation,
        ActivateProvider,
        LockUser,
        UnlockUser,
        RevokeUserSession,
        RevokeAllUserSessions,
        AssignUserRole,
        RemoveUserRole
    };
}
