namespace BackendProjectTemplate.Application.Authentication.Features.GetLoginActivityHistory;

public sealed record GetLoginActivityHistoryResult(
    GetLoginActivityHistoryStatus Status,
    IReadOnlyList<GetLoginActivityHistoryResponse> Activities,
    string? NextCursor);
