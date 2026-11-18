using BackendProjectTemplate.Application.Common.Pagination;
using BackendProjectTemplate.Domain.Payments.ReadModels;

namespace BackendProjectTemplate.Application.Payments.Features.GetAdminPayments;

public sealed class GetAdminPaymentsHandler(IPaymentAdministrationReadModelRepository readModelRepository)
{
    public async Task<GetAdminPaymentsResult> HandleAsync(GetAdminPaymentsCommand command, CancellationToken cancellationToken)
    {
        var (cursorCreatedAtUtc, cursorPaymentTransactionId) = CursorPagination.Decode(command.Cursor);
        var page = await readModelRepository.ListPaymentsAsync(
            new AdminPaymentListRequest(
                command.TenantId,
                command.StakeholderId,
                command.PaymentProviderId,
                command.CurrencyId,
                command.PaymentIntent,
                command.PaymentStatus,
                command.PaymentMethodType,
                command.CreatedFromUtc,
                command.CreatedToUtc,
                NormalizeSearch(command.Search),
                cursorCreatedAtUtc,
                cursorPaymentTransactionId,
                command.Limit),
            cancellationToken);

        var items = page.Items.Select(item => new AdminPaymentSummaryResponse(
            item.TransactionId,
            item.MerchantReference,
            item.ProviderReference,
            item.TenantId,
            item.StakeholderId,
            item.StakeholderDisplayName,
            item.PaymentProviderId,
            item.PaymentProviderName,
            item.PaymentProviderKey,
            item.Amount,
            item.CurrencyId,
            item.CurrencyCode,
            item.PaymentIntent.ToString(),
            item.PaymentMethodType.ToString(),
            item.PaymentStatus.ToString(),
            item.CreatedAtUtc,
            item.CompletedAtUtc,
            item.FailedAtUtc)).ToArray();
        var nextCursor = page.HasMore
            ? CursorPagination.Encode(page.Items[^1].CreatedAtUtc, page.Items[^1].TransactionId)
            : null;
        return new GetAdminPaymentsResult(items, nextCursor);
    }

    private static string? NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();
}
