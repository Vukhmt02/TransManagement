using TransManagement.Domain.Common;
using TransManagement.Domain.Enums;

namespace TransManagement.Domain.Entities;

public sealed class Vehicle : AuditableEntity
{
    private Vehicle()
    {
    }

    public Vehicle(string licensePlate, string vehicleType, decimal maxLoadKg, decimal? cargoVolumeM3 = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(licensePlate);
        ArgumentException.ThrowIfNullOrWhiteSpace(vehicleType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLoadKg);

        if (cargoVolumeM3 is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cargoVolumeM3));
        }

        LicensePlate = licensePlate.Trim().ToUpperInvariant();
        VehicleType = vehicleType.Trim();
        MaxLoadKg = maxLoadKg;
        CargoVolumeM3 = cargoVolumeM3;
    }

    public string LicensePlate { get; private set; } = string.Empty;

    public string VehicleType { get; private set; } = string.Empty;

    public decimal MaxLoadKg { get; private set; }

    public decimal? CargoVolumeM3 { get; private set; }

    public DateOnly? RegistrationExpiryDate { get; private set; }

    public DateOnly? InsuranceExpiryDate { get; private set; }

    public VehicleStatus Status { get; private set; } = VehicleStatus.Available;

    public ICollection<Shipment> Shipments { get; private set; } = [];

    public void Update(
        string vehicleType,
        decimal maxLoadKg,
        decimal? cargoVolumeM3,
        DateOnly? registrationExpiryDate,
        DateOnly? insuranceExpiryDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vehicleType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLoadKg);
        if (cargoVolumeM3 is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cargoVolumeM3));
        }

        VehicleType = vehicleType.Trim();
        MaxLoadKg = maxLoadKg;
        CargoVolumeM3 = cargoVolumeM3;
        RegistrationExpiryDate = registrationExpiryDate;
        InsuranceExpiryDate = insuranceExpiryDate;
    }

    public void SetStatus(VehicleStatus status) => Status = status;
}
