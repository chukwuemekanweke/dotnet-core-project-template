using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;

public sealed record GetStakeholderWalletTopUpTransactionDetailCommand(
    Guid WalletTransactionId,
    ActorContext ActorContext);