using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransManagement.API.Contracts.Common;
using TransManagement.Application.Common.Security;
using TransManagement.Application.Transport;
using TransManagement.Domain.Enums;

namespace TransManagement.API.Controllers;

[Authorize]
public sealed class VehiclesController(ITransportService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] VehicleStatus? status, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<VehicleDto>>.Ok(await service.GetVehiclesAsync(status, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetVehicleAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<VehicleDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateVehicleCommand command, CancellationToken cancellationToken)
    {
        var result = await service.CreateVehicleAsync(command, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse<VehicleDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateVehicleCommand command, CancellationToken cancellationToken)
    {
        var result = await service.UpdateVehicleAsync(id, command, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<VehicleDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, SetVehicleStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SetVehicleStatusAsync(id, request.Status, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
    }
}

public sealed record SetVehicleStatusRequest(VehicleStatus Status);
