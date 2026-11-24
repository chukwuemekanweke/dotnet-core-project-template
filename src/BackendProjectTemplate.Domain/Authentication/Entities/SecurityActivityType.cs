namespace BackendProjectTemplate.Domain.Authentication.Entities;

public enum SecurityActivityType
{
    InitialLogin = 0,
    TokenRefresh = 1,
    FailedSignIn = 2,
    PasskeyAdded = 3,
    PasskeyRemoved = 4,
    TwoFactorEnabled = 5,
    TwoFactorDisabled = 6,
    TwoFactorRecoveryCodesRegenerated = 7,
    PasswordChanged = 8
}
