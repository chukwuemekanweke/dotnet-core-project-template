namespace BackendProjectTemplate.Domain.Common.Authentication;

public enum GoogleAuthenticationFlowStatus
{
    Success = 1,
    Invalid = 2,
    Expired = 3,
    Consumed = 4
}
