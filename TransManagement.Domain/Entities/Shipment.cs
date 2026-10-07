using TransManagement.Domain.Common;
using TransManagement.Domain.Enums;

namespace TransManagement.Domain.Entities;

public sealed class Shipment : AuditableEntity
{
    private Shipment()
    {
    }

    public Shipment(string code, Guid vehicleId, Guid driverId, DateTime plannedDepartureAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException("Vehicle ID is required.", nameof(vehicleId));
        }

        if (driverId == Guid.Empty)
        {
            throw new ArgumentException("Driver ID is required.", nameof(driverId));
        }

        Code = code.Trim();
        VehicleId = vehicleId;
        DriverId = driverId;
        PlannedDepartureAtUtc = plannedDepartureAtUtc;
    }

    public string Code { get; private set; } = string.Empty;

    public Guid VehicleId { get; private set; }

    public Vehicle Vehicle { get; private set; } = null!;

    public Guid DriverId { get; private set; }

    public Driver Driver { get; private set; } = null!;

    public DateTime PlannedDepartureAtUtc { get; private set; }

    public DateTime? StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public ShipmentStatus Status { get; private set; } = ShipmentStatus.Planned;

    public string? Note { get; private set; }

    public ICollection<ShipmentOrder> ShipmentOrders { get; private set; } = [];

    public ICollection<RouteStop> RouteStops { get; private set; } = [];

    public void Assign()
    {
        if (Status != ShipmentStatus.Planned) throw new InvalidOperationException("Shipment is not planned.");
        Status = ShipmentStatus.Assigned;
    }

    public void Start(DateTime startedAtUtc)
    {
        if (Status != ShipmentStatus.Assigned) throw new InvalidOperationException("Only assigned shipments can start.");
        StartedAtUtc = startedAtUtc;
        Status = ShipmentStatus.Started;
    }

    public void Complete(DateTime completedAtUtc)
    {
        if (Status != ShipmentStatus.Started) throw new InvalidOperationException("Only started shipments can complete.");
        CompletedAtUtc = completedAtUtc;
        Status = ShipmentStatus.Completed;
    }

    public void Cancel()
    {
        if (Status is ShipmentStatus.Started or ShipmentStatus.Completed or ShipmentStatus.Cancelled)
        {
            throw new InvalidOperationException("This shipment cannot be cancelled.");
        }

        Status = ShipmentStatus.Cancelled;
    }
}
