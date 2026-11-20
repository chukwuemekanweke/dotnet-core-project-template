namespace BackendProjectTemplate.Domain.Analytics.ReadModels;

public sealed record ActiveUserListPage(
    IReadOnlyList<ActiveUserListItem> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
