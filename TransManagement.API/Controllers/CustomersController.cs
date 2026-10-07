using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransManagement.API.Contracts.Common;
using TransManagement.Application.Common.Security;
using TransManagement.Application.Transport;

namespace TransManagement.API.Controllers;

[Authorize]
public sealed class CustomersController(ITransportService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerDto>>>> GetAll([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<CustomerDto>>.Ok(await service.GetCustomersAsync(search, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetCustomerAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<CustomerDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var result = await service.CreateCustomerAsync(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse<CustomerDto>.Ok(result.Value))
            : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        var result = await service.UpdateCustomerAsync(id, command, cancellationToken);
        return result.IsSuccess ? Ok(ApiResponse<CustomerDto>.Ok(result.Value)) : ErrorResponse(result.Error);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Dispatcher}")]
    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, SetActiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SetCustomerActiveAsync(id, request.IsActive, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
    }
}

public sealed record SetActiveRequest(bool IsActive);
