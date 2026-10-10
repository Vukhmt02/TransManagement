using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TransManagement.API.Contracts.Common;
using TransManagement.API.Hubs;
using TransManagement.Application.Common.Security;
using TransManagement.Application.Transport;

namespace TransManagement.API.Controllers;

[Authorize(Roles = AppRoles.Driver)]
[Route("api/driver-portal")]
public sealed class DriverPortalController(ITransportService service, IHubContext<TrackingHub> trackingHub) : ApiControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.GetDriverByUserIdAsync(userId, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<DriverDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [HttpGet("shipments")]
    public async Task<IActionResult> Shipments(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(ApiResponse<IReadOnlyList<ShipmentDto>>.Ok(await service.GetDriverShipmentsAsync(userId, cancellationToken)));
    }

    [HttpGet("shipments/{id:guid}")]
    public async Task<IActionResult> Shipment(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.GetDriverShipmentAsync(userId, id, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<ShipmentDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [HttpPost("shipments/{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.StartDriverShipmentAsync(userId, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
    }

    [HttpPost("shipments/{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.CompleteDriverShipmentAsync(userId, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
    }

    [HttpPost("shipments/{id:guid}/location")]
    public async Task<IActionResult> Location(Guid id, UpdateShipmentLocationCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.AddShipmentLocationAsync(userId, id, command, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result.Error);
        await trackingHub.Clients.Group(TrackingHub.GroupName(id)).SendAsync("locationUpdated", result.Value, cancellationToken);
        return Ok(ApiResponse<ShipmentLocationDto>.Ok(result.Value));
    }

    [HttpPost("stops/{id:guid}/arrive")]
    public async Task<IActionResult> Arrive(Guid id, ArriveAtStopCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.ArriveAtStopAsync(userId, id, command, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<RouteStopDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [HttpPost("stops/{id:guid}/complete")]
    public async Task<IActionResult> CompleteStop(Guid id, CreateDeliveryProofCommand? command, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.CompleteStopAsync(userId, id, command, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<RouteStopDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [HttpPost("stops/{id:guid}/skip")]
    public async Task<IActionResult> Skip(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.SkipStopAsync(userId, id, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<RouteStopDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
