namespace BackendProjectTemplate.Contracts.Events;

public static class UserSecurityActivityTypes
{
    public const string PasskeyAdded = "passkey_added";
    public const string PasskeyRemoved = "passkey_removed";
    public const string TwoFactorEnabled = "two_factor_enabled";
    public const string TwoFactorDisabled = "two_factor_disabled";
    public const string TwoFactorRecoveryCodesRegenerated = "two_factor_recovery_codes_regenerated";
    public const string PasswordChanged = "password_changed";
}
