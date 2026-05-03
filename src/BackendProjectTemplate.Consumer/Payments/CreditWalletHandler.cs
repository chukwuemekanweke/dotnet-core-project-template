using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Payments;

public sealed class CreditWalletHandler(
    Domain.Common.Observability.ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<Wallet> walletRepository,
    IRepository<WalletTransaction> walletTransactionRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : BaseMessageHandler<CreditWalletCommand>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(CreditWalletCommand message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            CustomTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.ValueGrantFailed,
                ObservabilityEventProperties.CreatePayment(
                    CurrentActorAccessor,
                    Observability.StepNames.ValueGrant,
                    Observability.Outcomes.Failure,
                    failureReason: ObservabilityFailureReasons.StakeholderNotFound,
                    paymentReference: message.MerchantReference,
                    amount: message.Amount,
                    currencyId: message.CurrencyId,
                    source: Observability.Sources.Subscriber));
            throw new CannotProcessMessageNonTransientException("CreditWalletCommand must contain a valid stakeholder id.");
        }

        var existingWalletTransaction = await walletTransactionRepository.FirstOrDefaultAsync(
            new WalletTransactionByPaymentTransactionSpecification(message.PaymentTransactionId),
            cancellationToken);
        if (existingWalletTransaction is not null)
        {
            CustomTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.ValueGrantFailed,
                ObservabilityEventProperties.CreatePayment(
                    CurrentActorAccessor,
                    Observability.StepNames.ValueGrant,
                    Observability.Outcomes.Duplicate,
                    message.StakeholderId,
                    ObservabilityFailureReasons.DuplicateProcessing,
                    paymentReference: message.MerchantReference,
                    amount: message.Amount,
                    currencyId: message.CurrencyId,
                    source: Observability.Sources.Subscriber,
                    isDuplicate: true));
            return;
        }

        var wallet = await walletRepository.FirstOrDefaultAsync(
            new WalletByStakeholderAndCurrencySpecification(message.StakeholderId.Value, message.CurrencyId),
            cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (wallet is null)
        {
            wallet = Wallet.Create(message.StakeholderId.Value, message.TenantId, message.CurrencyId, now);
            wallet.Credit(message.Amount);
            await walletRepository.AddAsync(wallet, cancellationToken);
        }
        else
        {
            wallet.Credit(message.Amount);
            walletRepository.Update(wallet);
        }

        var walletFundingNarrative = WalletTransactionNarratives.WalletFunding;

        await walletTransactionRepository.AddAsync(
            WalletTransaction.CreateCredit(
                wallet.Id,
                message.PaymentTransactionId,
                message.MerchantReference,
                message.Amount,
                message.CurrencyId,
                now,
                WalletTransactionCategory.WalletFunding,
                walletFundingNarrative.Title,
                walletFundingNarrative.CreateDescription()),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.ValueGranted,
            ObservabilityEventProperties.CreatePayment(
                CurrentActorAccessor,
                Observability.StepNames.ValueGrant,
                Observability.Outcomes.Success,
                message.StakeholderId,
                paymentReference: message.MerchantReference,
                amount: message.Amount,
                currencyId: message.CurrencyId,
                source: Observability.Sources.Subscriber));
    }
}
