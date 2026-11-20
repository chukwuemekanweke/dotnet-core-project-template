namespace BackendProjectTemplate.Application.Authentication.Features.RefreshSession;

public enum RefreshSessionStatus
{
    Success = 1,
    InvalidRefreshToken = 2,
    EmailNotVerified = 3,
    AccountLocked = 4
}
