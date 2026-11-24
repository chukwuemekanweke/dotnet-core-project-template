namespace BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;

public sealed record GetSecurityActivityHistoryResult(
    GetSecurityActivityHistoryStatus Status,
    IReadOnlyList<GetSecurityActivityHistoryResponse> Activities,
    string? NextCursor);
