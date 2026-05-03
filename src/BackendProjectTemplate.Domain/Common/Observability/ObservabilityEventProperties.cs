using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Common.Observability;

public static class ObservabilityEventProperties
{
    public static Dictionary<string, string> Create(
        ActorContext actorContext,
        Guid? stakeholderId = null,
        string? failureReason = null,
        IReadOnlyDictionary<string, string>? additionalProperties = null)
    {
        var properties = new Dictionary<string, string>
        {
            [Observability.CorrelationIdPropertyName] = string.IsNullOrWhiteSpace(actorContext.CorrelationId)
                ? Guid.CreateVersion7().ToString("N")
                : actorContext.CorrelationId,
            [Observability.FlowIdPropertyName] = actorContext.FlowId ?? string.Empty
        };

        if (actorContext.TenantId.HasValue)
        {
            properties[Observability.TenantIdPropertyName] = actorContext.TenantId.Value.ToString();
        }

        if (stakeholderId.HasValue)
        {
            properties[Observability.StakeholderIdPropertyName] = stakeholderId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(failureReason))
        {
            properties[Observability.FailureReasonPropertyName] = failureReason;
        }

        if (additionalProperties is null)
        {
            return properties;
        }

        foreach (var property in additionalProperties)
        {
            properties[property.Key] = property.Value;
        }

        return properties;
    }

    public static Dictionary<string, string> Create(
        ICurrentActor currentActor,
        Guid? stakeholderId = null,
        string? failureReason = null,
        IReadOnlyDictionary<string, string>? additionalProperties = null)
    {
        var actorContext = ActorContext.FromAnonymousActor(currentActor);
        return Create(actorContext, stakeholderId, failureReason, additionalProperties);
    }

    public static Dictionary<string, string> CreatePayment(
        ActorContext actorContext,
        string stepName,
        string outcome,
        Guid? stakeholderId = null,
        string? failureReason = null,
        string? provider = null,
        string? paymentReference = null,
        PaymentMethodType? paymentMethodType = null,
        PaymentIntent? paymentIntent = null,
        decimal? amount = null,
        Guid? currencyId = null,
        string? source = null,
        string? terminalState = null,
        bool? isDuplicate = null,
        int? deliveryAttempt = null,
        int? retryCount = null,
        string? exceptionType = null,
        double? durationMs = null,
        IReadOnlyDictionary<string, string>? additionalProperties = null) =>
        Create(
            actorContext,
            stakeholderId,
            failureReason,
            MergeAdditionalProperties(
                CreatePaymentProperties(
                    stepName,
                    outcome,
                    provider,
                    paymentReference,
                    paymentMethodType,
                    paymentIntent,
                    amount,
                    currencyId,
                    source,
                    terminalState,
                    isDuplicate,
                    deliveryAttempt,
                    retryCount,
                    exceptionType,
                    durationMs),
                additionalProperties));

    public static Dictionary<string, string> CreatePayment(
        ICurrentActor currentActor,
        string stepName,
        string outcome,
        Guid? stakeholderId = null,
        string? failureReason = null,
        string? provider = null,
        string? paymentReference = null,
        PaymentMethodType? paymentMethodType = null,
        PaymentIntent? paymentIntent = null,
        decimal? amount = null,
        Guid? currencyId = null,
        string? source = null,
        string? terminalState = null,
        bool? isDuplicate = null,
        int? deliveryAttempt = null,
        int? retryCount = null,
        string? exceptionType = null,
        double? durationMs = null,
        IReadOnlyDictionary<string, string>? additionalProperties = null) =>
        Create(
            currentActor,
            stakeholderId,
            failureReason,
            MergeAdditionalProperties(
                CreatePaymentProperties(
                    stepName,
                    outcome,
                    provider,
                    paymentReference,
                    paymentMethodType,
                    paymentIntent,
                    amount,
                    currencyId,
                    source,
                    terminalState,
                    isDuplicate,
                    deliveryAttempt,
                    retryCount,
                    exceptionType,
                    durationMs),
                additionalProperties));

    public static Dictionary<string, string> CreatePayment(
        string stepName,
        string outcome,
        Guid? stakeholderId = null,
        string? failureReason = null,
        string? provider = null,
        string? paymentReference = null,
        PaymentMethodType? paymentMethodType = null,
        PaymentIntent? paymentIntent = null,
        decimal? amount = null,
        Guid? currencyId = null,
        string? source = null,
        string? terminalState = null,
        bool? isDuplicate = null,
        int? deliveryAttempt = null,
        int? retryCount = null,
        string? exceptionType = null,
        double? durationMs = null,
        IReadOnlyDictionary<string, string>? additionalProperties = null)
    {
        var properties = CreatePaymentProperties(
            stepName,
            outcome,
            provider,
            paymentReference,
            paymentMethodType,
            paymentIntent,
            amount,
            currencyId,
            source,
            terminalState,
            isDuplicate,
            deliveryAttempt,
            retryCount,
            exceptionType,
            durationMs);

        if (!string.IsNullOrWhiteSpace(failureReason))
        {
            properties[Observability.FailureReasonPropertyName] = failureReason;
        }

        if (stakeholderId.HasValue)
        {
            properties[Observability.StakeholderIdPropertyName] = stakeholderId.Value.ToString();
        }

        return MergeAdditionalProperties(properties, additionalProperties);
    }

    private static Dictionary<string, string> CreatePaymentProperties(
        string stepName,
        string outcome,
        string? provider,
        string? paymentReference,
        PaymentMethodType? paymentMethodType,
        PaymentIntent? paymentIntent,
        decimal? amount,
        Guid? currencyId,
        string? source,
        string? terminalState,
        bool? isDuplicate,
        int? deliveryAttempt,
        int? retryCount,
        string? exceptionType,
        double? durationMs)
    {
        var properties = CreateFlowProperties(
            Observability.FlowNames.Payments,
            stepName,
            outcome,
            source);

        if (!string.IsNullOrWhiteSpace(provider))
        {
            properties[Observability.ProviderPropertyName] = provider;
        }

        if (!string.IsNullOrWhiteSpace(paymentReference))
        {
            properties[Observability.PaymentReferencePropertyName] = paymentReference;
        }

        if (paymentMethodType.HasValue)
        {
            properties[Observability.PaymentMethodPropertyName] = paymentMethodType.Value.ToString();
        }

        if (paymentIntent.HasValue)
        {
            properties[Observability.PaymentIntentPropertyName] = paymentIntent.Value.ToString();
        }

        if (amount.HasValue)
        {
            properties[Observability.AmountBucketPropertyName] = GetAmountBucket(amount.Value);
        }

        if (currencyId.HasValue)
        {
            properties[Observability.CurrencyIdPropertyName] = currencyId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(terminalState))
        {
            properties[Observability.TerminalStatePropertyName] = terminalState;
        }

        if (isDuplicate.HasValue)
        {
            properties[Observability.IsDuplicatePropertyName] = isDuplicate.Value.ToString();
        }

        if (deliveryAttempt.HasValue)
        {
            properties[Observability.DeliveryAttemptPropertyName] = deliveryAttempt.Value.ToString();
        }

        if (retryCount.HasValue)
        {
            properties[Observability.RetryCountPropertyName] = retryCount.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(exceptionType))
        {
            properties[Observability.ExceptionTypePropertyName] = exceptionType;
        }

        if (durationMs.HasValue)
        {
            properties[Observability.DurationMsPropertyName] = durationMs.Value.ToString("0.###");
        }

        return properties;
    }

    private static Dictionary<string, string> CreateFlowProperties(
        string flowName,
        string stepName,
        string outcome,
        string? source)
    {
        var properties = new Dictionary<string, string>
        {
            [Observability.FlowNamePropertyName] = flowName,
            [Observability.StepNamePropertyName] = stepName,
            [Observability.OutcomePropertyName] = outcome
        };

        if (!string.IsNullOrWhiteSpace(source))
        {
            properties[Observability.SourcePropertyName] = source;
        }

        return properties;
    }

    private static Dictionary<string, string> MergeAdditionalProperties(
        Dictionary<string, string> properties,
        IReadOnlyDictionary<string, string>? additionalProperties)
    {
        if (additionalProperties is null)
        {
            return properties;
        }

        foreach (var property in additionalProperties)
        {
            properties[property.Key] = property.Value;
        }

        return properties;
    }

    private static string GetAmountBucket(decimal amount) =>
        amount switch
        {
            < 1_000m => "lt_1000",
            < 5_000m => "1000_to_4999",
            < 20_000m => "5000_to_19999",
            < 100_000m => "20000_to_99999",
            _ => "100000_plus"
        };
}
