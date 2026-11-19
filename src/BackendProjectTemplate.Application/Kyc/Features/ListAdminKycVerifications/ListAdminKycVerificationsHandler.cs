using BackendProjectTemplate.Application.Common.Pagination;
using BackendProjectTemplate.Domain.Kyc.ReadModels;

namespace BackendProjectTemplate.Application.Kyc.Features.ListAdminKycVerifications;

public sealed class ListAdminKycVerificationsHandler(IKycAdministrationReadModelRepository readModelRepository)
{
    public async Task<ListAdminKycVerificationsResult> HandleAsync(
        ListAdminKycVerificationsCommand command,
        CancellationToken cancellationToken)
    {
        var (cursorStartedAtUtc, cursorVerificationId) = CursorPagination.Decode(command.Cursor);
        var page = await readModelRepository.ListVerificationsAsync(
            new AdminKycVerificationListRequest(
                command.TenantId,
                command.StakeholderId,
                command.Status,
                Normalize(command.ProviderKey),
                Normalize(command.ProviderStatus),
                Normalize(command.ProviderReference),
                Normalize(command.Search),
                command.StartedFromUtc,
                command.StartedToUtc,
                cursorStartedAtUtc,
                cursorVerificationId,
                command.Limit),
            cancellationToken);

        var items = page.Items.Select(item => new AdminKycVerificationSummaryResponse(
            item.VerificationId,
            item.TenantId,
            item.StakeholderId,
            item.StakeholderDisplayName,
            item.ProviderKey,
            item.Status.ToString(),
            item.LatestProviderStatus,
            item.ProviderVerificationReference,
            item.StartedAtUtc,
            item.SubmittedAtUtc,
            item.CompletedAtUtc,
            item.LastReconciledAtUtc)).ToArray();
        var nextCursor = page.HasMore
            ? CursorPagination.Encode(page.Items[^1].StartedAtUtc, page.Items[^1].VerificationId)
            : null;
        return new ListAdminKycVerificationsResult(items, nextCursor);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
