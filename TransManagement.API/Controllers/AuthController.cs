using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransManagement.API.Contracts.Auth;
using TransManagement.API.Contracts.Common;
using TransManagement.Application.Common.Identity;
using TransManagement.Application.Common.Interfaces;
using TransManagement.Application.Common.Models;

namespace TransManagement.API.Controllers;

public sealed class AuthController(IIdentityService identityService) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("register/request-otp")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RequestRegistrationOtp(
        RequestRegistrationOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await identityService.RequestRegistrationOtpAsync(request.Email, cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : ErrorResponse(result.Error,
                result.Error.Code == "auth.otp_rate_limited" ? StatusCodes.Status429TooManyRequests : StatusCodes.Status400BadRequest,
                "Could not send verification code");
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokens>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(
        VerifyRegistrationOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await identityService.VerifyRegistrationOtpAndRegisterAsync(
            request.Email,
            request.Otp,
            request.Password,
            request.FullName,
            request.Phone,
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<AuthTokens>.Ok(result.Value, "Registration successful."))
            : ErrorResponse(result.Error, StatusCodes.Status409Conflict, "Registration failed");
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokens>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await identityService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<AuthTokens>.Ok(result.Value, "Login successful."))
            : ErrorResponse(result.Error, StatusCodes.Status401Unauthorized, "Login failed");
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokens>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshToken(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await identityService.RefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<AuthTokens>.Ok(result.Value, "Token refreshed."))
            : ErrorResponse(result.Error, StatusCodes.Status401Unauthorized, "Token refresh failed");
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await identityService.RevokeRefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ErrorResponse(result.Error, StatusCodes.Status400BadRequest, "Logout failed");
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await identityService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ErrorResponse(result.Error, StatusCodes.Status400BadRequest, "Password change failed");
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfile>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await identityService.GetProfileAsync(userId, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<UserProfile>.Ok(result.Value))
            : ErrorResponse(result.Error, StatusCodes.Status404NotFound, "User not found");
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    private ObjectResult ErrorResponse(Error error, int statusCode, string title)
    {
        var details = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = error.Description,
            Instance = HttpContext.Request.Path
        };

        details.Extensions["code"] = error.Code;
        details.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return StatusCode(statusCode, details);
    }
}
