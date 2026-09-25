namespace BackendProjectTemplate.Domain.Common.Authentication;

public enum TwoFactorChallengeStatus
{
    Success = 1,
    Invalid = 2,
    Expired = 3,
    Consumed = 4,
    Exhausted = 5
}
