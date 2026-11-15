using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.WebAPI.Infrastructure.StepUpMfa;

public sealed class StepUpMfaOperationRegistry
{
    private static readonly IReadOnlyDictionary<string, OperationContract> Contracts =
        new Dictionary<string, OperationContract>(StringComparer.Ordinal)
        {
            [StepUpMfaOperations.AllocateSubscription] = new("POST", "/api/v1/admin/subscriptions/allocations"),
            [StepUpMfaOperations.CancelSubscriptionImmediately] = new("POST", "/api/v1/admin/subscriptions/{id}/immediate-cancellation"),
            [StepUpMfaOperations.CancelSubscriptionAtPeriodEnd] = new("POST", "/api/v1/admin/subscriptions/{id}/cancellation-at-period-end"),
            [StepUpMfaOperations.RetrySubscriptionPurchaseFulfillment] = new("POST", "/api/v1/admin/subscriptions/{id}/purchase-fulfillment-retries"),
            [StepUpMfaOperations.CreateSubscriptionPlan] = new("POST", "/api/v1/admin/subscription-plans"),
            [StepUpMfaOperations.UpdateSubscriptionPlan] = new("PUT", "/api/v1/admin/subscription-plans/{id}"),
            [StepUpMfaOperations.SetSubscriptionPlanActivation] = new("PUT", "/api/v1/admin/subscription-plans/{id}/activation"),
            [StepUpMfaOperations.SetPaymentProviderActivation] = new("PUT", "/api/v1/payment-providers/{id}/activation"),
            [StepUpMfaOperations.ActivateProvider] = new("PUT", "/api/v1/providers/active")
        };

    public bool Matches(string operation, string method, string path) =>
        Contracts.TryGetValue(operation, out var contract) && contract.Matches(method, path);

    public bool TryResolveOperation(string method, string path, out string operation)
    {
        foreach (var contract in Contracts)
        {
            if (contract.Value.Matches(method, path))
            {
                operation = contract.Key;
                return true;
            }
        }

        operation = string.Empty;
        return false;
    }

    private sealed record OperationContract(string Method, string PathTemplate)
    {
        public bool Matches(string method, string path)
        {
            if (!string.Equals(Method, method, StringComparison.OrdinalIgnoreCase))
                return false;

            var expected = PathTemplate.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var actual = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (expected.Length != actual.Length)
                return false;

            for (var index = 0; index < expected.Length; index++)
            {
                if (expected[index] == "{id}")
                {
                    if (!Guid.TryParse(actual[index], out _))
                        return false;
                    continue;
                }

                if (!string.Equals(expected[index], actual[index], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
    }
}
