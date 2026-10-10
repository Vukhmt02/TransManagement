using TransManagement.Domain.Common;

namespace TransManagement.Domain.Entities;

public sealed class ShipmentLocation : AuditableEntity
{
    private ShipmentLocation() { }

    public ShipmentLocation(Guid shipmentId, Guid driverId, decimal latitude, decimal longitude,
        decimal? speedKph, decimal? accuracyMeters, DateTime recordedAtUtc)
    {
        if (shipmentId == Guid.Empty) throw new ArgumentException("Shipment ID is required.", nameof(shipmentId));
        if (driverId == Guid.Empty) throw new ArgumentException("Driver ID is required.", nameof(driverId));
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
        if (speedKph is < 0) throw new ArgumentOutOfRangeException(nameof(speedKph));
        if (accuracyMeters is < 0) throw new ArgumentOutOfRangeException(nameof(accuracyMeters));

        ShipmentId = shipmentId;
        DriverId = driverId;
        Latitude = latitude;
        Longitude = longitude;
        SpeedKph = speedKph;
        AccuracyMeters = accuracyMeters;
        RecordedAtUtc = recordedAtUtc;
    }

    public Guid ShipmentId { get; private set; }
    public Shipment Shipment { get; private set; } = null!;
    public Guid DriverId { get; private set; }
    public Driver Driver { get; private set; } = null!;
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public decimal? SpeedKph { get; private set; }
    public decimal? AccuracyMeters { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
}
