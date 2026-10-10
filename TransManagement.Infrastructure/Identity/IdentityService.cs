using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TransManagement.Application.Common.Identity;
using TransManagement.Application.Common.Interfaces;
using TransManagement.Application.Common.Models;
using TransManagement.Application.Common.Security;
using TransManagement.Domain.Entities;
using TransManagement.Infrastructure.Persistence;

namespace TransManagement.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IEmailSender emailSender,
    IOptions<JwtOptions> jwtOptions) : IIdentityService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result> RequestRegistrationOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        if (await userManager.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken))
            return Result.Failure(IdentityErrors.RegistrationFailed("This email is already registered."));

        var now = DateTime.UtcNow;
        var entity = await dbContext.RegistrationOtps.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (entity is not null && entity.LastSentAtUtc > now.AddSeconds(-60))
            return Result.Failure(IdentityErrors.OtpRateLimited);

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var hash = HashOtp(normalizedEmail, otp, salt);
        if (entity is null)
        {
            entity = new RegistrationOtp(normalizedEmail, hash, salt, now);
            dbContext.RegistrationOtps.Add(entity);
        }
        else entity.Replace(hash, salt, now);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendRegistrationOtpAsync(email.Trim(), otp, cancellationToken);
            return Result.Success();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            entity.AllowImmediateRetry();
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure(IdentityErrors.EmailDeliveryFailed);
        }
    }

    public async Task<Result<AuthTokens>> VerifyRegistrationOtpAndRegisterAsync(
        string email, string otp, string password, string fullName, string phone,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var entity = await dbContext.RegistrationOtps.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (entity is null || entity.IsUsed) return Result<AuthTokens>.Failure(IdentityErrors.OtpInvalid);
        if (entity.ExpiresAtUtc <= DateTime.UtcNow) return Result<AuthTokens>.Failure(IdentityErrors.OtpExpired);
        if (entity.FailedAttempts >= 5) return Result<AuthTokens>.Failure(IdentityErrors.OtpAttemptsExceeded);

        var actualHash = HashOtp(normalizedEmail, otp.Trim(), entity.Salt);
        var matches = CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(entity.CodeHash), Convert.FromBase64String(actualHash));
        if (!matches)
        {
            entity.RecordFailure();
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<AuthTokens>.Failure(entity.FailedAttempts >= 5
                ? IdentityErrors.OtpAttemptsExceeded : IdentityErrors.OtpInvalid);
        }

        var result = await RegisterAsync(email, password, fullName, phone, cancellationToken);
        if (!result.IsSuccess) return result;
        entity.MarkUsed();
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<Result<AuthTokens>> RegisterAsync(
        string email,
        string password,
        string fullName,
        string phone,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser
        {
            Email = email.Trim(),
            UserName = email.Trim(),
            FullName = fullName.Trim(),
            PhoneNumber = phone.Trim()
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return Result<AuthTokens>.Failure(
                IdentityErrors.RegistrationFailed(JoinErrors(createResult)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Customer);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Result<AuthTokens>.Failure(
                IdentityErrors.RegistrationFailed(JoinErrors(roleResult)));
        }

        var customer = new Customer(
            $"CUS-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            fullName,
            phone,
            email);
        customer.LinkToUser(user.Id);
        dbContext.Customers.Add(customer);

        var tokens = await IssueTokensAsync(user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<AuthTokens>.Success(tokens);
    }

    public async Task<Result<AuthTokens>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return Result<AuthTokens>.Failure(IdentityErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result<AuthTokens>.Failure(IdentityErrors.AccountInactive);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return Result<AuthTokens>.Failure(IdentityErrors.AccountLocked);
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);

            return await userManager.IsLockedOutAsync(user)
                ? Result<AuthTokens>.Failure(IdentityErrors.AccountLocked)
                : Result<AuthTokens>.Failure(IdentityErrors.InvalidCredentials);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        return Result<AuthTokens>.Success(
            await IssueTokensAsync(user, cancellationToken));
    }

    public async Task<Result<AuthTokens>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive || !storedToken.User.IsActive)
        {
            return Result<AuthTokens>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        storedToken.Revoke();

        return Result<AuthTokens>.Success(
            await IssueTokensAsync(storedToken.User, cancellationToken));
    }

    public async Task<Result> RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return Result.Failure(IdentityErrors.InvalidRefreshToken);
        }

        storedToken.Revoke();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        var changeResult = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!changeResult.Succeeded)
        {
            return Result.Failure(
                IdentityErrors.PasswordChangeFailed(JoinErrors(changeResult)));
        }

        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<UserProfile>> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<UserProfile>.Failure(IdentityErrors.UserNotFound);
        }

        var roles = await userManager.GetRolesAsync(user);

        return Result<UserProfile>.Success(
            new UserProfile(user.Id, user.Email!, user.FullName, roles.ToArray()));
    }

    public async Task<Result> SetUserActiveStatusAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        user.IsActive = isActive;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result.Failure(
                new Error("auth.user_update_failed", JoinErrors(updateResult)));
        }

        if (!isActive)
        {
            await userManager.UpdateSecurityStampAsync(user);
            await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
        }

        return Result.Success();
    }

    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        var result = new List<UserSummary>(users.Count);
        foreach (var user in users)
            result.Add(new UserSummary(user.Id, user.Email!, user.FullName, user.IsActive, (await userManager.GetRolesAsync(user)).ToArray()));
        return result;
    }

    public async Task<Result<UserSummary>> SetUserRolesAsync(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default)
    {
        var normalized = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (normalized.Length == 0 || normalized.Any(role => !AppRoles.All.Contains(role, StringComparer.OrdinalIgnoreCase)))
            return Result<UserSummary>.Failure(new Error("auth.invalid_roles", "One or more roles are invalid."));
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result<UserSummary>.Failure(IdentityErrors.UserNotFound);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var current = await userManager.GetRolesAsync(user);
        var remove = await userManager.RemoveFromRolesAsync(user, current);
        if (!remove.Succeeded) return Result<UserSummary>.Failure(new Error("auth.user_update_failed", JoinErrors(remove)));
        var add = await userManager.AddToRolesAsync(user, normalized);
        if (!add.Succeeded) return Result<UserSummary>.Failure(new Error("auth.user_update_failed", JoinErrors(add)));
        await userManager.UpdateSecurityStampAsync(user);
        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result<UserSummary>.Success(new UserSummary(user.Id, user.Email!, user.FullName, user.IsActive, normalized));
    }

    private async Task<AuthTokens> IssueTokensAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var accessTokenExpiresAtUtc = now.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var roles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new("security_stamp", user.SecurityStamp!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(
                JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(now).ToString(),
                ClaimValueTypes.Integer64)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            now,
            accessTokenExpiresAtUtc,
            credentials);

        var rawRefreshToken = CreateRefreshToken();
        var storedRefreshToken = new RefreshToken(
            user.Id,
            HashToken(rawRefreshToken),
            now.AddDays(_jwtOptions.RefreshTokenDays));

        dbContext.RefreshTokens.Add(storedRefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokens(
            new JwtSecurityTokenHandler().WriteToken(jwt),
            rawRefreshToken,
            accessTokenExpiresAtUtc);
    }

    private static string HashOtp(string normalizedEmail, string otp, string salt)
    {
        var bytes = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes($"{normalizedEmail}:{otp}"),
            Convert.FromBase64String(salt),
            100_000,
            HashAlgorithmName.SHA256,
            32);
        return Convert.ToBase64String(bytes);
    }

    private async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string JoinErrors(Microsoft.AspNetCore.Identity.IdentityResult result)
    {
        return string.Join(" ", result.Errors.Select(error => error.Description));
    }
}
