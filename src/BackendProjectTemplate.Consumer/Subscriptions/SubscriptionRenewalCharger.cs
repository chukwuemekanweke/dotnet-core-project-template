using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Microsoft.Extensions.Options;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Consumer.Subscriptions;

public sealed class SubscriptionRenewalCharger(
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<Currency> currencyRepository,
    IEnumerable<IRecurringPaymentProvider> recurringPaymentProviders,
    IRecurringPaymentAuthorizationProtector authorizationProtector,
    IEventPublisher eventPublisher,
    ICommandSender commandSender,
    IOptions<SubscriptionRenewalOptions> options,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task ChargeAsync(
        SubscriptionRenewal renewal,
        SubscriptionAggregate subscription,
        RecurringPaymentAuthorization authorization,
        PaymentTransaction identitySource,
        CancellationToken cancellationToken)
    {
        var currency = await currencyRepository.GetByIdAsync(renewal.CurrencyId, cancellationToken)
            ?? throw new InvalidOperationException($"Currency '{renewal.CurrencyId}' was not found.");
        var provider = await paymentProviderRepository.GetByIdAsync(authorization.PaymentProviderId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment provider '{authorization.PaymentProviderId}' was not found.");
        var recurringProvider = recurringPaymentProviders.SingleOrDefault(candidate =>
            string.Equals(candidate.ProviderKey, provider.ProviderKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Payment provider '{provider.ProviderKey}' does not support recurring charges.");

        var now = timeProvider.GetUtcNow();
        var payment = PaymentTransaction.Create(
            $"pay_{Guid.CreateVersion7():N}",
            PaymentIntent.Subscription,
            provider.Id,
            renewal.ExpectedPrice,
            renewal.CurrencyId,
            identitySource.CountryId,
            identitySource.InitiatedByUserId,
            identitySource.StakeholderId,
            renewal.TenantId);
        var idempotencyKey = $"renewal-{renewal.Id:N}-{renewal.Attempts.Count + 1}";
        var attempt = renewal.StartAttempt(authorization.Id, payment.Id, idempotencyKey, now);
        await paymentTransactionRepository.AddAsync(payment, cancellationToken);

        // Persist the attempt, transaction and stable idempotency key before crossing the network boundary.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        RecurringChargeResult result;
        try
        {
            result = await recurringProvider.ChargeAsync(
                new RecurringChargeRequest(
                    payment.MerchantReference,
                    payment.Amount,
                    currency.CurrencyCode,
                    authorization.ProviderCustomerReference,
                    authorizationProtector.Unprotect(authorization.AuthorizationReference!),
                    idempotencyKey,
                    new Dictionary<string, string>
                    {
                        ["subscription_id"] = subscription.Id.ToString(),
                        ["renewal_id"] = renewal.Id.ToString(),
                        ["renewal_attempt_id"] = attempt.Id.ToString(),
                        ["merchant_reference"] = payment.MerchantReference
                    }),
                cancellationToken);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            exception is HttpRequestException or TaskCanceledException)
        {
            attempt.MarkUnknown("The Bachs charge outcome is unknown.");
            renewal.MarkAwaitingReconciliation();
            await ScheduleReconciliationAsync(renewal, attempt, identitySource, cancellationToken);
            return;
        }

        await ApplyResultAsync(renewal, attempt, payment, result, identitySource, cancellationToken);
    }

    public async Task ReconcileAsync(
        SubscriptionRenewal renewal,
        SubscriptionRenewalAttempt attempt,
        PaymentTransaction payment,
        RecurringPaymentAuthorization authorization,
        CancellationToken cancellationToken)
    {
        if (attempt.Status is SubscriptionRenewalAttemptStatus.Succeeded or SubscriptionRenewalAttemptStatus.Failed)
        {
            return;
        }

        if (payment.PaymentStatus == PaymentStatus.Succeeded)
        {
            attempt.MarkSucceeded(timeProvider.GetUtcNow());
            return;
        }
        if (payment.PaymentStatus is PaymentStatus.Failed or PaymentStatus.Expired or PaymentStatus.Cancelled)
        {
            await HandleFailureAsync(
                renewal,
                attempt,
                new RecurringChargeResult(
                    RecurringChargeStatus.Failed,
                    payment.ProviderReference,
                    "provider_reported_failure",
                    payment.FailureReason),
                payment,
                cancellationToken);
            return;
        }

        var currency = await currencyRepository.GetByIdAsync(payment.CurrencyId, cancellationToken)
            ?? throw new InvalidOperationException($"Currency '{payment.CurrencyId}' was not found.");
        var provider = await paymentProviderRepository.GetByIdAsync(payment.PaymentProviderId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment provider '{payment.PaymentProviderId}' was not found.");
        var recurringProvider = recurringPaymentProviders.Single(candidate =>
            string.Equals(candidate.ProviderKey, provider.ProviderKey, StringComparison.OrdinalIgnoreCase));

        RecurringChargeResult result;
        try
        {
            result = string.IsNullOrWhiteSpace(payment.ProviderReference)
                ? await recurringProvider.ChargeAsync(
                    new RecurringChargeRequest(
                        payment.MerchantReference,
                        payment.Amount,
                        currency.CurrencyCode,
                        authorization.ProviderCustomerReference,
                        authorizationProtector.Unprotect(authorization.AuthorizationReference!),
                        attempt.ProviderIdempotencyKey,
                        new Dictionary<string, string>
                        {
                            ["subscription_id"] = renewal.SubscriptionId.ToString(),
                            ["renewal_id"] = renewal.Id.ToString(),
                            ["renewal_attempt_id"] = attempt.Id.ToString(),
                            ["merchant_reference"] = payment.MerchantReference
                        }),
                    cancellationToken)
                : await recurringProvider.GetChargeAsync(
                    new RecurringChargeVerificationRequest(
                        payment.ProviderReference,
                        payment.MerchantReference,
                        payment.Amount,
                        currency.CurrencyCode),
                    cancellationToken);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            exception is HttpRequestException or TaskCanceledException)
        {
            attempt.MarkUnknown("The Bachs charge outcome remains unknown.");
            renewal.MarkAwaitingReconciliation();
            await ScheduleReconciliationAsync(renewal, attempt, payment, cancellationToken);
            return;
        }

        await ApplyResultAsync(renewal, attempt, payment, result, payment, cancellationToken);
    }

    private async Task ApplyResultAsync(
        SubscriptionRenewal renewal,
        SubscriptionRenewalAttempt attempt,
        PaymentTransaction payment,
        RecurringChargeResult result,
        PaymentTransaction identitySource,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (!string.IsNullOrWhiteSpace(result.ProviderReference) && payment.PaymentStatus == PaymentStatus.PendingInitiation)
        {
            payment.MarkInitiated(
                result.ProviderReference,
                new Dictionary<string, string> { ["renewal_attempt_id"] = attempt.Id.ToString() },
                null,
                KnownPaymentTransactionChangeReasons.PaymentInitiated);
            payment.SetPaymentMethodType(PaymentMethodType.Card);
            attempt.MarkSubmitted(now);
        }

        switch (result.Status)
        {
            case RecurringChargeStatus.Succeeded:
                payment.MarkSucceeded(result.ProviderReference, KnownPaymentTransactionChangeReasons.ReconciliationConfirmedSuccess, now);
                attempt.MarkSucceeded(now);
                await eventPublisher.PublishAsync(
                    new SuccessfulPaymentConfirmed
                    {
                        PaymentTransactionId = payment.Id,
                        MerchantReference = payment.MerchantReference,
                        PaymentIntent = payment.PaymentIntent,
                        PaymentProviderId = payment.PaymentProviderId,
                        Amount = payment.Amount,
                        CurrencyId = payment.CurrencyId,
                        StakeholderId = payment.StakeholderId,
                        TenantId = payment.TenantId,
                        FlowId = string.Empty,
                        OccuredAt = now
                    },
                    cancellationToken);
                break;
            case RecurringChargeStatus.Failed:
                payment.MarkFailed(result.ProviderReference, result.FailureReason, KnownPaymentTransactionChangeReasons.ReconciliationConfirmedFailure, now);
                await HandleFailureAsync(renewal, attempt, result, identitySource, cancellationToken);
                break;
            default:
                renewal.MarkAwaitingReconciliation();
                await ScheduleReconciliationAsync(renewal, attempt, identitySource, cancellationToken);
                break;
        }
    }

    private async Task HandleFailureAsync(
        SubscriptionRenewal renewal,
        SubscriptionRenewalAttempt attempt,
        RecurringChargeResult result,
        PaymentTransaction identitySource,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var maximumAttempts = Math.Max(1, options.Value.MaximumRetryAttempts);
        if (attempt.AttemptNumber >= maximumAttempts)
        {
            attempt.MarkFailed(result.FailureCode, result.FailureReason, now, null);
            renewal.MarkExhausted(now);
            return;
        }

        var backoff = options.Value.RetryBackoff.ElementAtOrDefault(attempt.AttemptNumber - 1);
        if (backoff <= TimeSpan.Zero)
        {
            backoff = TimeSpan.FromDays(1);
        }

        var retryAt = now.Add(backoff);
        attempt.MarkFailed(result.FailureCode, result.FailureReason, now, retryAt);
        renewal.MarkRetryScheduled(retryAt);
        await commandSender.ScheduleSendAsync(
            new RetrySubscriptionRenewalCommand(renewal.Id, attempt.AttemptNumber)
            {
                TenantId = renewal.TenantId,
                StakeholderId = identitySource.StakeholderId
            },
            retryAt,
            cancellationToken);
    }

    private Task ScheduleReconciliationAsync(
        SubscriptionRenewal renewal,
        SubscriptionRenewalAttempt attempt,
        PaymentTransaction identitySource,
        CancellationToken cancellationToken) =>
        commandSender.ScheduleSendAsync(
            new ReconcileSubscriptionRenewalCommand(renewal.Id, attempt.Id)
            {
                TenantId = renewal.TenantId,
                StakeholderId = identitySource.StakeholderId
            },
            timeProvider.GetUtcNow().Add(options.Value.ReconciliationDelay),
            cancellationToken);
}
