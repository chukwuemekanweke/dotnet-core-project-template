namespace BackendProjectTemplate.Domain.Authentication;

public enum AdministrativeSecurityAction
{
    LockUser = 1,
    UnlockUser = 2,
    RevokeSession = 3,
    RevokeAllSessions = 4,
    AssignRole = 5,
    RemoveRole = 6
}
