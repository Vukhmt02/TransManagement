using TransManagement.Application.Common.Identity;
using TransManagement.Application.Common.Models;

namespace TransManagement.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<AuthTokens>> RegisterAsync(
        string email,
        string password,
        string fullName,
        string phone,
        CancellationToken cancellationToken = default);

    Task<Result<AuthTokens>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<Result<AuthTokens>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<Result> RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<Result> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<Result<UserProfile>> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result> SetUserActiveStatusAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
