namespace BackendProjectTemplate.Domain.Authentication.Administration;

public sealed record AdminUserListPage(
    IReadOnlyList<AdminUserListItem> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
