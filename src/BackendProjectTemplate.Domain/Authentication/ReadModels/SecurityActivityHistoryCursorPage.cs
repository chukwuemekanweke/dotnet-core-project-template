namespace BackendProjectTemplate.Domain.Authentication.ReadModels;

public sealed record SecurityActivityHistoryCursorPage(
    IReadOnlyList<SecurityActivityHistoryReadModel> Activities,
    bool HasMore);
