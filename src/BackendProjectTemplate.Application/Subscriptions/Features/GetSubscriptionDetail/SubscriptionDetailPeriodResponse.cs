namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;

public sealed record SubscriptionDetailPeriodResponse(
    Guid PeriodId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    string PlanCode,
    decimal NominalPrice,
    Guid CurrencyId,
    string BillingInterval,
    string ActivationSource,
    Guid? PaymentTransactionId,
    Guid OperationId);
