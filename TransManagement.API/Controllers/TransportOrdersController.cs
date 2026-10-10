using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TransManagement.API.Contracts.Common;
using TransManagement.Application.Common.Security;
using TransManagement.Application.Transport;
using TransManagement.Domain.Enums;

namespace TransManagement.API.Controllers;

[Authorize]
public sealed class TransportOrdersController(ITransportService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    public async Task<IActionResult> GetAll([FromQuery] TransportOrderStatus? status, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<TransportOrderDto>>.Ok(await service.GetOrdersAsync(status, cancellationToken)));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
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

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(ApiResponse<IReadOnlyList<TransportOrderDto>>.Ok(await service.GetCustomerOrdersAsync(userId, cancellationToken)));
    }

    [HttpPost("mine")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> CreateMine(CreateCustomerOrderCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.CreateCustomerOrderAsync(userId, command, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<TransportOrderDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [HttpPost("mine/{id:guid}/cancel")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> CancelMine(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.CancelCustomerOrderAsync(userId, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

public sealed record SetOrderStatusRequest(TransportOrderStatus Status);
