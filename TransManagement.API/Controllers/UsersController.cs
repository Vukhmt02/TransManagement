using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransManagement.API.Contracts.Auth;
using TransManagement.Application.Common.Interfaces;
using TransManagement.Application.Common.Security;

namespace TransManagement.API.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public sealed class UsersController(IIdentityService identityService) : ApiControllerBase
{
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
}
