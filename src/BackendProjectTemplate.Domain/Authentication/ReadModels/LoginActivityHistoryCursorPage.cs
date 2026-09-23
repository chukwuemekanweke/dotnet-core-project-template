namespace BackendProjectTemplate.Domain.Authentication.ReadModels;

public sealed record LoginActivityHistoryCursorPage(
    IReadOnlyList<LoginActivityHistoryReadModel> Activities,
    bool HasMore);
