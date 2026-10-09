using TransManagement.Application.Common.Models;
using TransManagement.Domain.Enums;

namespace TransManagement.Application.Transport;

public interface ITransportService
{
    Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(string? search, CancellationToken cancellationToken);
    Task<Result<CustomerDto>> GetCustomerAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<CustomerDto>> GetCustomerByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result<CustomerDto>> CreateCustomerAsync(CreateCustomerCommand command, CancellationToken cancellationToken);
    Task<Result<CustomerDto>> UpdateCustomerAsync(Guid id, UpdateCustomerCommand command, CancellationToken cancellationToken);
    Task<Result> SetCustomerActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken);

    Task<IReadOnlyList<DriverDto>> GetDriversAsync(DriverStatus? status, CancellationToken cancellationToken);
    Task<Result<DriverDto>> GetDriverAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<DriverDto>> CreateDriverAsync(CreateDriverCommand command, CancellationToken cancellationToken);
    Task<Result<DriverDto>> UpdateDriverAsync(Guid id, UpdateDriverCommand command, CancellationToken cancellationToken);
    Task<Result> SetDriverStatusAsync(Guid id, DriverStatus status, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleDto>> GetVehiclesAsync(VehicleStatus? status, CancellationToken cancellationToken);
    Task<Result<VehicleDto>> GetVehicleAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<VehicleDto>> CreateVehicleAsync(CreateVehicleCommand command, CancellationToken cancellationToken);
    Task<Result<VehicleDto>> UpdateVehicleAsync(Guid id, UpdateVehicleCommand command, CancellationToken cancellationToken);
    Task<Result> SetVehicleStatusAsync(Guid id, VehicleStatus status, CancellationToken cancellationToken);

    Task<IReadOnlyList<TransportOrderDto>> GetOrdersAsync(TransportOrderStatus? status, CancellationToken cancellationToken);
    Task<Result<TransportOrderDto>> GetOrderAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<TransportOrderDto>> CreateOrderAsync(CreateTransportOrderCommand command, CancellationToken cancellationToken);
    Task<Result<TransportOrderDto>> UpdateOrderAsync(Guid id, UpdateTransportOrderCommand command, CancellationToken cancellationToken);
    Task<Result> ChangeOrderStatusAsync(Guid id, TransportOrderStatus status, CancellationToken cancellationToken);

    Task<IReadOnlyList<ShipmentDto>> GetShipmentsAsync(ShipmentStatus? status, CancellationToken cancellationToken);
    Task<Result<ShipmentDto>> GetShipmentAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<ShipmentDto>> CreateShipmentAsync(CreateShipmentCommand command, CancellationToken cancellationToken);
    Task<Result> StartShipmentAsync(Guid id, CancellationToken cancellationToken);
    Task<Result> CompleteShipmentAsync(Guid id, CancellationToken cancellationToken);
    Task<Result> CancelShipmentAsync(Guid id, CancellationToken cancellationToken);
}
