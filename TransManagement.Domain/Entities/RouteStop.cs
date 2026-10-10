using TransManagement.Domain.Common;
using TransManagement.Domain.Enums;

namespace TransManagement.Domain.Entities;

public sealed class RouteStop : AuditableEntity
{
    private RouteStop()
    {
    }

    public RouteStop(
        Guid shipmentId,
        RouteStopType type,
        int sequence,
        string address,
        Guid? transportOrderId = null)
    {
        if (shipmentId == Guid.Empty)
        {
            throw new ArgumentException("Shipment ID is required.", nameof(shipmentId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequence);

        ShipmentId = shipmentId;
        Type = type;
        Sequence = sequence;
        Address = address.Trim();
        TransportOrderId = transportOrderId;
    }

    public Guid ShipmentId { get; private set; }

    public Shipment Shipment { get; private set; } = null!;

    public Guid? TransportOrderId { get; private set; }

    public TransportOrder? TransportOrder { get; private set; }

    public RouteStopType Type { get; private set; }

    public int Sequence { get; private set; }

    public string Address { get; private set; } = string.Empty;

    public string? ContactName { get; private set; }

    public string? ContactPhone { get; private set; }

    public decimal? Latitude { get; private set; }

    public decimal? Longitude { get; private set; }

    public DateTime? PlannedArrivalAtUtc { get; private set; }

    public DateTime? ArrivedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public RouteStopStatus Status { get; private set; } = RouteStopStatus.Pending;

    public DeliveryProof? DeliveryProof { get; private set; }

    public void Arrive(decimal latitude, decimal longitude, DateTime arrivedAtUtc)
    {
        if (Status != RouteStopStatus.Pending) throw new InvalidOperationException("Only pending stops can be marked as arrived.");
        ValidateCoordinates(latitude, longitude);
        Latitude = latitude;
        Longitude = longitude;
        ArrivedAtUtc = arrivedAtUtc;
        Status = RouteStopStatus.Arrived;
    }

    public void Complete(DateTime completedAtUtc)
    {
        if (Status != RouteStopStatus.Arrived) throw new InvalidOperationException("The driver must arrive before completing a stop.");
        CompletedAtUtc = completedAtUtc;
        Status = RouteStopStatus.Completed;
    }

    public void Skip(DateTime completedAtUtc)
    {
        if (Status is RouteStopStatus.Completed or RouteStopStatus.Skipped)
            throw new InvalidOperationException("This stop has already been closed.");
        CompletedAtUtc = completedAtUtc;
        Status = RouteStopStatus.Skipped;
    }

    private static void ValidateCoordinates(decimal latitude, decimal longitude)
    {
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
    }
}
