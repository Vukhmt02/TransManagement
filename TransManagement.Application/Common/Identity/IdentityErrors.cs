using TransManagement.Application.Common.Models;

namespace TransManagement.Application.Common.Identity;

public static class IdentityErrors
{
    public static readonly Error InvalidCredentials =
        new("auth.invalid_credentials", "Email or password is incorrect.");

    public static readonly Error AccountLocked =
        new("auth.account_locked", "The account is temporarily locked.");

    public static readonly Error AccountInactive =
        new("auth.account_inactive", "The account is inactive.");

    public static readonly Error InvalidRefreshToken =
        new("auth.invalid_refresh_token", "The refresh token is invalid or expired.");

    public static readonly Error UserNotFound =
        new("auth.user_not_found", "The user was not found.");

    public static Error RegistrationFailed(string description) =>
        new("auth.registration_failed", description);

    public static Error PasswordChangeFailed(string description) =>
        new("auth.password_change_failed", description);
}

