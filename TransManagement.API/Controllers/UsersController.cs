using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransManagement.API.Contracts.Auth;
using TransManagement.Application.Common.Interfaces;
using TransManagement.Application.Common.Security;
using TransManagement.API.Contracts.Common;
using TransManagement.Application.Common.Identity;

namespace TransManagement.API.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public sealed class UsersController(IIdentityService identityService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<UserSummary>>.Ok(await identityService.GetUsersAsync(cancellationToken)));

    [HttpPost("driver-accounts")]
    [ProducesResponseType(typeof(ApiResponse<UserSummary>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDriverAccount(
        CreateDriverAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await identityService.CreateDriverAccountAsync(
            request.DriverId,
            request.Email,
            request.TemporaryPassword,
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse<UserSummary>.Ok(result.Value))
            : ErrorResponse(result.Error);
    }

    [HttpPut("{userId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetStatus(
        Guid userId,
        SetUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.IsActive &&
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId) &&
            currentUserId == userId)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid operation",
                detail: "An administrator cannot deactivate their own account.");
        }

        var result = await identityService.SetUserActiveStatusAsync(
            userId,
            request.IsActive,
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "User not found",
                detail: result.Error.Description);
    }

    [HttpPut("{userId:guid}/roles")]
    public async Task<IActionResult> SetRoles(Guid userId, SetUserRolesRequest request, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId) && currentUserId == userId)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid operation", detail: "An administrator cannot change their own roles.");
        var result = await identityService.SetUserRolesAsync(userId, request.Roles, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponse<UserSummary>.Ok(result.Value))
            : Problem(statusCode: StatusCodes.Status400BadRequest, title: "Role update failed", detail: result.Error.Description);
    }
}

public sealed record SetUserRolesRequest(IReadOnlyCollection<string> Roles);
