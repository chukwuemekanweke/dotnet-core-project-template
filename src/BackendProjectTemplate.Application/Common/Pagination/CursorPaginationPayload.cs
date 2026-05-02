namespace BackendProjectTemplate.Application.Common.Pagination;

public sealed record CursorPaginationPayload(long CreatedAtUnixMilliseconds, Guid EntityId);
