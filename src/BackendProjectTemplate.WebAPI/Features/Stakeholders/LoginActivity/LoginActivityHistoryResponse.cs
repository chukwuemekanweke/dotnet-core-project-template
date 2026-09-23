using BackendProjectTemplate.Application.Authentication.Features.GetLoginActivityHistory;

namespace BackendProjectTemplate.WebAPI.Features.Stakeholders.LoginActivity;

public sealed record LoginActivityHistoryResponse(
    IReadOnlyList<GetLoginActivityHistoryResponse> Activities,
    string? NextCursor);
