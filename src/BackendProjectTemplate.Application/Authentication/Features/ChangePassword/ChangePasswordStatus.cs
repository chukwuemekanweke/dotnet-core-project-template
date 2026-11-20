namespace BackendProjectTemplate.Application.Authentication.Features.ChangePassword;

public enum ChangePasswordStatus
{
    Success = 1,
    NotAuthenticated = 2,
    UserNotFound = 3,
    IncorrectCurrentPassword = 4,
    ValidationFailed = 5
}
