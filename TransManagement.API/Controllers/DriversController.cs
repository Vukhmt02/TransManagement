using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransManagement.API.Contracts.Common;
using TransManagement.Application.Common.Security;
using TransManagement.Application.Transport;
using TransManagement.Domain.Enums;

namespace TransManagement.API.Controllers;

[Authorize]
public sealed class DriversController(ITransportService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DriverStatus? status, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<DriverDto>>.Ok(await service.GetDriversAsync(status, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetDriverAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<DriverDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateDriverCommand command, CancellationToken cancellationToken)
    {
        var result = await service.CreateDriverAsync(command, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse<DriverDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateDriverCommand command, CancellationToken cancellationToken)
    {
        var result = await service.UpdateDriverAsync(id, command, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<DriverDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, SetDriverStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SetDriverStatusAsync(id, request.Status, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
    }
}

public sealed record SetDriverStatusRequest(DriverStatus Status);
