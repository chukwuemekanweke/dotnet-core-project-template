using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Payments.Features.GetStakeholderWalletTransactions;

public sealed record GetStakeholderWalletTransactionsCommand(
    int Limit,
    string? Cursor,
    ActorContext ActorContext);
