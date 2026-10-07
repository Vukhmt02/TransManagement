using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransManagement.API.Contracts.Common;
using TransManagement.Application.Common.Security;
using TransManagement.Application.Transport;
using TransManagement.Domain.Enums;

namespace TransManagement.API.Controllers;

[Authorize]
public sealed class ShipmentsController(ITransportService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ShipmentStatus? status, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<ShipmentDto>>.Ok(await service.GetShipmentsAsync(status, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetShipmentAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<ShipmentDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateShipmentCommand command, CancellationToken cancellationToken)
    {
        var result = await service.CreateShipmentAsync(command, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse<ShipmentDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher},{AppRoles.Driver}")]
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.StartShipmentAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher},{AppRoles.Driver}")]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.CompleteShipmentAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.CancelShipmentAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
    }
}
