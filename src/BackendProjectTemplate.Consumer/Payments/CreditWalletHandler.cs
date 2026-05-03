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
            throw new CannotProcessMessageNonTransientException("CreditWalletCommand must contain a valid stakeholder id.");
        }

        var existingWalletTransaction = await walletTransactionRepository.FirstOrDefaultAsync(
            new WalletTransactionByPaymentTransactionSpecification(message.PaymentTransactionId),
            cancellationToken);
        if (existingWalletTransaction is not null)
        {
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
            CustomTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.WalletCreated,
                ObservabilityEventProperties.Create(
                    CurrentActorAccessor,
                    message.StakeholderId,
                    additionalProperties: new Dictionary<string, string>
                    {
                        [Observability.CurrencyIdPropertyName] = message.CurrencyId.ToString(),
                        [Observability.WalletIdPropertyName] = wallet.Id.ToString()
                    }));
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
            Observability.EventNames.Payments.WalletCredited,
            ObservabilityEventProperties.Create(
                CurrentActorAccessor,
                message.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PaymentReferencePropertyName] = message.MerchantReference,
                    [Observability.CurrencyIdPropertyName] = message.CurrencyId.ToString(),
                    [Observability.WalletIdPropertyName] = wallet.Id.ToString()
                }));
    }
}
