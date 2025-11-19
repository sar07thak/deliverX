namespace DeliverX.Domain.Enums;

public static class AuthEventType
{
    public const string OtpSent = "OTP_SENT";
    public const string OtpVerified = "OTP_VERIFIED";
    public const string OtpFailed = "OTP_FAILED";
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string SessionCreated = "SESSION_CREATED";
    public const string SessionRevoked = "SESSION_REVOKED";
    public const string TokenRefreshed = "TOKEN_REFRESHED";
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string TwoFAEnabled = "2FA_ENABLED";
    public const string TwoFADisabled = "2FA_DISABLED";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string AccountUnlocked = "ACCOUNT_UNLOCKED";
}
