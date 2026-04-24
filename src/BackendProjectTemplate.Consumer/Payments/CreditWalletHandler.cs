using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
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
            await walletRepository.AddAsync(wallet, cancellationToken);
        }

        wallet.Credit(message.Amount);
        walletRepository.Update(wallet);

        await walletTransactionRepository.AddAsync(
            WalletTransaction.CreateCredit(
                wallet.Id,
                message.PaymentTransactionId,
                message.MerchantReference,
                message.Amount,
                message.CurrencyId,
                now),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
