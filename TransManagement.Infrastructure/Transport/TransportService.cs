using Microsoft.EntityFrameworkCore;
using TransManagement.Application.Common.Models;
using TransManagement.Application.Transport;
using TransManagement.Domain.Entities;
using TransManagement.Domain.Enums;
using TransManagement.Infrastructure.Persistence;

namespace TransManagement.Infrastructure.Transport;

public sealed class TransportService(ApplicationDbContext dbContext) : ITransportService
{
    private static readonly Error NotFound = new("Transport.NotFound", "The requested resource was not found.");
    private static readonly Error Conflict = new("Transport.Conflict", "A resource with the same unique value already exists.");
    private static readonly Error Invalid = new("Transport.Invalid", "The requested operation is not valid for the current state.");

    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(string? search, CancellationToken cancellationToken)
    {
        var query = dbContext.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Code.ToLower().Contains(term) || x.Name.ToLower().Contains(term) || x.Phone.Contains(term));
        }

        return (await query.OrderBy(x => x.Code).ToListAsync(cancellationToken)).Select(Map).ToList();
    }

    public async Task<Result<CustomerDto>> GetCustomerAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? Result<CustomerDto>.Failure(NotFound) : Result<CustomerDto>.Success(Map(entity));
    }

    public async Task<Result<CustomerDto>> CreateCustomerAsync(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        if (await dbContext.Customers.AnyAsync(x => x.Code == command.Code.Trim(), cancellationToken))
            return Result<CustomerDto>.Failure(Conflict);

        var entity = new Customer(command.Code, command.Name, command.Phone, command.Email);
        entity.Update(command.Name, command.Phone, command.Email, command.TaxCode, command.Address);
        dbContext.Customers.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<CustomerDto>.Success(Map(entity));
    }

    public async Task<Result<CustomerDto>> UpdateCustomerAsync(Guid id, UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Customers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Result<CustomerDto>.Failure(NotFound);
        entity.Update(command.Name, command.Phone, command.Email, command.TaxCode, command.Address);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<CustomerDto>.Success(Map(entity));
    }

    public async Task<Result> SetCustomerActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Customers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Result.Failure(NotFound);
        entity.SetActive(isActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<DriverDto>> GetDriversAsync(DriverStatus? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Drivers.AsNoTracking();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return (await query.OrderBy(x => x.EmployeeCode).ToListAsync(cancellationToken)).Select(Map).ToList();
    }

    public async Task<Result<DriverDto>> GetDriverAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Drivers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? Result<DriverDto>.Failure(NotFound) : Result<DriverDto>.Success(Map(entity));
    }

    public async Task<Result<DriverDto>> CreateDriverAsync(CreateDriverCommand command, CancellationToken cancellationToken)
    {
        if (await dbContext.Drivers.AnyAsync(x => x.EmployeeCode == command.EmployeeCode.Trim() || x.LicenseNumber == command.LicenseNumber.Trim(), cancellationToken))
            return Result<DriverDto>.Failure(Conflict);

        var entity = new Driver(command.EmployeeCode, command.FullName, command.Phone, command.LicenseNumber, command.LicenseClass, command.LicenseExpiryDate);
        dbContext.Drivers.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<DriverDto>.Success(Map(entity));
    }

    public async Task<Result<DriverDto>> UpdateDriverAsync(Guid id, UpdateDriverCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Drivers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Result<DriverDto>.Failure(NotFound);
        if (await dbContext.Drivers.AnyAsync(x => x.Id != id && x.LicenseNumber == command.LicenseNumber.Trim(), cancellationToken))
            return Result<DriverDto>.Failure(Conflict);
        entity.Update(command.FullName, command.Phone, command.LicenseNumber, command.LicenseClass, command.LicenseExpiryDate);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<DriverDto>.Success(Map(entity));
    }

    public async Task<Result> SetDriverStatusAsync(Guid id, DriverStatus status, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Drivers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Result.Failure(NotFound);
        if (entity.Status == DriverStatus.Assigned || status == DriverStatus.Assigned) return Result.Failure(Invalid);
        entity.SetStatus(status);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<VehicleDto>> GetVehiclesAsync(VehicleStatus? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Vehicles.AsNoTracking();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return (await query.OrderBy(x => x.LicensePlate).ToListAsync(cancellationToken)).Select(Map).ToList();
    }

    public async Task<Result<VehicleDto>> GetVehicleAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Vehicles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? Result<VehicleDto>.Failure(NotFound) : Result<VehicleDto>.Success(Map(entity));
    }

    public async Task<Result<VehicleDto>> CreateVehicleAsync(CreateVehicleCommand command, CancellationToken cancellationToken)
    {
        var plate = command.LicensePlate.Trim().ToUpperInvariant();
        if (await dbContext.Vehicles.AnyAsync(x => x.LicensePlate == plate, cancellationToken)) return Result<VehicleDto>.Failure(Conflict);
        var entity = new Vehicle(command.LicensePlate, command.VehicleType, command.MaxLoadKg, command.CargoVolumeM3);
        entity.Update(command.VehicleType, command.MaxLoadKg, command.CargoVolumeM3, command.RegistrationExpiryDate, command.InsuranceExpiryDate);
        dbContext.Vehicles.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<VehicleDto>.Success(Map(entity));
    }

    public async Task<Result<VehicleDto>> UpdateVehicleAsync(Guid id, UpdateVehicleCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Result<VehicleDto>.Failure(NotFound);
        entity.Update(command.VehicleType, command.MaxLoadKg, command.CargoVolumeM3, command.RegistrationExpiryDate, command.InsuranceExpiryDate);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<VehicleDto>.Success(Map(entity));
    }

    public async Task<Result> SetVehicleStatusAsync(Guid id, VehicleStatus status, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Result.Failure(NotFound);
        if (entity.Status is VehicleStatus.Assigned or VehicleStatus.InTransit ||
            status is VehicleStatus.Assigned or VehicleStatus.InTransit)
            return Result.Failure(Invalid);
        entity.SetStatus(status);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<TransportOrderDto>> GetOrdersAsync(TransportOrderStatus? status, CancellationToken cancellationToken)
    {
        IQueryable<TransportOrder> query = dbContext.TransportOrders.AsNoTracking().Include(x => x.Customer);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return (await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken)).Select(Map).ToList();
    }

    public async Task<Result<TransportOrderDto>> GetOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TransportOrders.AsNoTracking().Include(x => x.Customer).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? Result<TransportOrderDto>.Failure(NotFound) : Result<TransportOrderDto>.Success(Map(entity));
    }

    public async Task<Result<TransportOrderDto>> CreateOrderAsync(CreateTransportOrderCommand command, CancellationToken cancellationToken)
    {
        if (await dbContext.TransportOrders.AnyAsync(x => x.Code == command.Code.Trim(), cancellationToken)) return Result<TransportOrderDto>.Failure(Conflict);
        var customer = await dbContext.Customers.SingleOrDefaultAsync(x => x.Id == command.CustomerId && x.IsActive, cancellationToken);
        if (customer is null) return Result<TransportOrderDto>.Failure(new Error("Transport.CustomerUnavailable", "Customer does not exist or is inactive."));

        var entity = new TransportOrder(command.Code, command.CustomerId, command.PickupAddress, command.DeliveryAddress, command.GoodsDescription, command.WeightKg, command.PackageCount);
        entity.UpdateDetails(command.PickupAddress, command.DeliveryAddress, command.GoodsDescription, command.WeightKg, command.PackageCount, command.VolumeM3, command.ExpectedPickupAtUtc, command.ExpectedDeliveryAtUtc, command.EstimatedPrice, command.SenderName, command.SenderPhone, command.RecipientName, command.RecipientPhone, command.Note);
        entity.ChangeStatus(TransportOrderStatus.WaitingForAssignment);
        dbContext.TransportOrders.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<TransportOrderDto>.Success(Map(entity));
    }

    public async Task<Result<TransportOrderDto>> UpdateOrderAsync(Guid id, UpdateTransportOrderCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TransportOrders.Include(x => x.Customer).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Result<TransportOrderDto>.Failure(NotFound);
        try
        {
            entity.UpdateDetails(command.PickupAddress, command.DeliveryAddress, command.GoodsDescription, command.WeightKg, command.PackageCount, command.VolumeM3, command.ExpectedPickupAtUtc, command.ExpectedDeliveryAtUtc, command.EstimatedPrice, command.SenderName, command.SenderPhone, command.RecipientName, command.RecipientPhone, command.Note);
        }
        catch (InvalidOperationException) { return Result<TransportOrderDto>.Failure(Invalid); }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<TransportOrderDto>.Success(Map(entity));
    }

    public async Task<Result> ChangeOrderStatusAsync(Guid id, TransportOrderStatus status, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TransportOrders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Result.Failure(NotFound);
        try { entity.ChangeStatus(status); }
        catch (InvalidOperationException) { return Result.Failure(Invalid); }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<ShipmentDto>> GetShipmentsAsync(ShipmentStatus? status, CancellationToken cancellationToken)
    {
        var query = ShipmentQuery();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return (await query.OrderByDescending(x => x.PlannedDepartureAtUtc).ToListAsync(cancellationToken)).Select(Map).ToList();
    }

    public async Task<Result<ShipmentDto>> GetShipmentAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await ShipmentQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? Result<ShipmentDto>.Failure(NotFound) : Result<ShipmentDto>.Success(Map(entity));
    }

    public async Task<Result<ShipmentDto>> CreateShipmentAsync(CreateShipmentCommand command, CancellationToken cancellationToken)
    {
        if (command.OrderIds.Count == 0 || command.OrderIds.Distinct().Count() != command.OrderIds.Count) return Result<ShipmentDto>.Failure(Invalid);
        if (await dbContext.Shipments.AnyAsync(x => x.Code == command.Code.Trim(), cancellationToken)) return Result<ShipmentDto>.Failure(Conflict);

        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == command.VehicleId, cancellationToken);
        var driver = await dbContext.Drivers.SingleOrDefaultAsync(x => x.Id == command.DriverId, cancellationToken);
        var orders = await dbContext.TransportOrders.Where(x => command.OrderIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (vehicle is null || driver is null || orders.Count != command.OrderIds.Count) return Result<ShipmentDto>.Failure(NotFound);
        if (vehicle.Status != VehicleStatus.Available || driver.Status != DriverStatus.Available || driver.LicenseExpiryDate < DateOnly.FromDateTime(command.PlannedDepartureAtUtc)) return Result<ShipmentDto>.Failure(Invalid);
        if (orders.Any(x => x.Status != TransportOrderStatus.WaitingForAssignment) || orders.Sum(x => x.WeightKg) > vehicle.MaxLoadKg) return Result<ShipmentDto>.Failure(Invalid);
        if (vehicle.CargoVolumeM3.HasValue && orders.Sum(x => x.VolumeM3 ?? 0) > vehicle.CargoVolumeM3.Value) return Result<ShipmentDto>.Failure(Invalid);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var shipment = new Shipment(command.Code, command.VehicleId, command.DriverId, command.PlannedDepartureAtUtc);
        shipment.Assign();
        dbContext.Shipments.Add(shipment);
        vehicle.SetStatus(VehicleStatus.Assigned);
        driver.SetStatus(DriverStatus.Assigned);

        for (var index = 0; index < command.OrderIds.Count; index++)
        {
            var order = orders.Single(x => x.Id == command.OrderIds[index]);
            order.ChangeStatus(TransportOrderStatus.Assigned);
            dbContext.ShipmentOrders.Add(new ShipmentOrder(shipment.Id, order.Id, index + 1));
            dbContext.RouteStops.Add(new RouteStop(shipment.Id, RouteStopType.Pickup, index * 2 + 1, order.PickupAddress, order.Id));
            dbContext.RouteStops.Add(new RouteStop(shipment.Id, RouteStopType.Delivery, index * 2 + 2, order.DeliveryAddress, order.Id));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetShipmentAsync(shipment.Id, cancellationToken);
    }

    public async Task<Result> StartShipmentAsync(Guid id, CancellationToken cancellationToken)
    {
        var shipment = await dbContext.Shipments.Include(x => x.Vehicle).Include(x => x.Driver).Include(x => x.ShipmentOrders).ThenInclude(x => x.TransportOrder).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (shipment is null) return Result.Failure(NotFound);
        try
        {
            shipment.Start(DateTime.UtcNow);
            shipment.Vehicle.SetStatus(VehicleStatus.InTransit);
            foreach (var item in shipment.ShipmentOrders)
            {
                item.TransportOrder.ChangeStatus(TransportOrderStatus.PickingUp);
                item.TransportOrder.ChangeStatus(TransportOrderStatus.InTransit);
            }
        }
        catch (InvalidOperationException) { return Result.Failure(Invalid); }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> CompleteShipmentAsync(Guid id, CancellationToken cancellationToken)
    {
        var shipment = await dbContext.Shipments.Include(x => x.Vehicle).Include(x => x.Driver).Include(x => x.ShipmentOrders).ThenInclude(x => x.TransportOrder).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (shipment is null) return Result.Failure(NotFound);
        try
        {
            shipment.Complete(DateTime.UtcNow);
            shipment.Vehicle.SetStatus(VehicleStatus.Available);
            shipment.Driver.SetStatus(DriverStatus.Available);
            foreach (var item in shipment.ShipmentOrders)
            {
                item.TransportOrder.ChangeStatus(TransportOrderStatus.Delivered);
                item.TransportOrder.ChangeStatus(TransportOrderStatus.Completed);
            }
        }
        catch (InvalidOperationException) { return Result.Failure(Invalid); }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> CancelShipmentAsync(Guid id, CancellationToken cancellationToken)
    {
        var shipment = await dbContext.Shipments.Include(x => x.Vehicle).Include(x => x.Driver).Include(x => x.ShipmentOrders).ThenInclude(x => x.TransportOrder).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (shipment is null) return Result.Failure(NotFound);
        try
        {
            shipment.Cancel();
            shipment.Vehicle.SetStatus(VehicleStatus.Available);
            shipment.Driver.SetStatus(DriverStatus.Available);
            foreach (var item in shipment.ShipmentOrders) item.TransportOrder.ChangeStatus(TransportOrderStatus.WaitingForAssignment);
        }
        catch (InvalidOperationException) { return Result.Failure(Invalid); }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private IQueryable<Shipment> ShipmentQuery() => dbContext.Shipments.AsNoTracking()
        .Include(x => x.Vehicle).Include(x => x.Driver)
        .Include(x => x.ShipmentOrders).ThenInclude(x => x.TransportOrder);

    private static CustomerDto Map(Customer x) => new(x.Id, x.Code, x.Name, x.Phone, x.Email, x.TaxCode, x.Address, x.IsActive);
    private static DriverDto Map(Driver x) => new(x.Id, x.EmployeeCode, x.FullName, x.Phone, x.LicenseNumber, x.LicenseClass, x.LicenseExpiryDate, x.Status);
    private static VehicleDto Map(Vehicle x) => new(x.Id, x.LicensePlate, x.VehicleType, x.MaxLoadKg, x.CargoVolumeM3, x.RegistrationExpiryDate, x.InsuranceExpiryDate, x.Status);
    private static TransportOrderDto Map(TransportOrder x) => new(x.Id, x.Code, x.CustomerId, x.Customer.Name, x.PickupAddress, x.DeliveryAddress, x.GoodsDescription, x.WeightKg, x.VolumeM3, x.PackageCount, x.ExpectedPickupAtUtc, x.ExpectedDeliveryAtUtc, x.EstimatedPrice, x.SenderName, x.SenderPhone, x.RecipientName, x.RecipientPhone, x.Note, x.Status);
    private static ShipmentDto Map(Shipment x) => new(x.Id, x.Code, x.VehicleId, x.Vehicle.LicensePlate, x.DriverId, x.Driver.FullName, x.PlannedDepartureAtUtc, x.StartedAtUtc, x.CompletedAtUtc, x.Status, x.ShipmentOrders.OrderBy(y => y.Sequence).Select(y => new ShipmentOrderDto(y.TransportOrderId, y.TransportOrder.Code, y.Sequence, y.TransportOrder.PickupAddress, y.TransportOrder.DeliveryAddress, y.TransportOrder.Status)).ToList());
}
