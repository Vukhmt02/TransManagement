using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransManagement.API.Contracts.Common;
using TransManagement.Application.Common.Security;
using TransManagement.Application.Transport;
using TransManagement.Domain.Enums;

namespace TransManagement.API.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
public sealed class TransportOrdersController(ITransportService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TransportOrderStatus? status, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<TransportOrderDto>>.Ok(await service.GetOrdersAsync(status, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetOrderAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<TransportOrderDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateTransportOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await service.CreateOrderAsync(command, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse<TransportOrderDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTransportOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await service.UpdateOrderAsync(id, command, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<TransportOrderDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, SetOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ChangeOrderStatusAsync(id, request.Status, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
    }
}

public sealed record SetOrderStatusRequest(TransportOrderStatus Status);
