namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionPeriodReadModel(
    Guid PeriodId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    string PlanCode,
    decimal NominalPrice,
    Guid CurrencyId,
    BillingInterval BillingInterval,
    SubscriptionActivationSource ActivationSource,
    Guid? PaymentTransactionId,
    Guid OperationId);
