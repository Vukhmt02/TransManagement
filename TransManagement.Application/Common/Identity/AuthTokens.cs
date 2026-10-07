namespace TransManagement.Application.Common.Identity;

public sealed record AuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc);

