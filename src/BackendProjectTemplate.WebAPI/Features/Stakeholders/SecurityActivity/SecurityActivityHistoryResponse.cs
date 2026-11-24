using BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;

namespace BackendProjectTemplate.WebAPI.Features.Stakeholders.SecurityActivity;

public sealed record SecurityActivityHistoryResponse(
    IReadOnlyList<GetSecurityActivityHistoryResponse> Activities,
    string? NextCursor);
