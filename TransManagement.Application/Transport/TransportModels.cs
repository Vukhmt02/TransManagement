using TransManagement.Domain.Enums;

namespace TransManagement.Application.Transport;

public sealed record CustomerDto(
    Guid Id, string Code, string Name, string Phone, string? Email,
    string? TaxCode, string? Address, bool IsActive, Guid? UserId);

public sealed record DriverDto(
    Guid Id, string EmployeeCode, string FullName, string Phone,
    string LicenseNumber, string LicenseClass, DateOnly LicenseExpiryDate,
    DriverStatus Status, Guid? UserId);

public sealed record VehicleDto(
    Guid Id, string LicensePlate, string VehicleType, decimal MaxLoadKg,
    decimal? CargoVolumeM3, DateOnly? RegistrationExpiryDate,
    DateOnly? InsuranceExpiryDate, VehicleStatus Status);

public sealed record TransportOrderDto(
    Guid Id, string Code, Guid CustomerId, string CustomerName,
    string PickupAddress, string DeliveryAddress, string GoodsDescription,
    decimal WeightKg, decimal? VolumeM3, int PackageCount,
    DateTime? ExpectedPickupAtUtc, DateTime? ExpectedDeliveryAtUtc,
    decimal? EstimatedPrice, string? SenderName, string? SenderPhone,
    string? RecipientName, string? RecipientPhone, string? Note,
    TransportOrderStatus Status);

public sealed record ShipmentDto(
    Guid Id, string Code, Guid VehicleId, string LicensePlate,
    Guid DriverId, string DriverName, DateTime PlannedDepartureAtUtc,
    DateTime? StartedAtUtc, DateTime? CompletedAtUtc, ShipmentStatus Status,
    IReadOnlyList<ShipmentOrderDto> Orders, IReadOnlyList<RouteStopDto> RouteStops,
    ShipmentLocationDto? LatestLocation);

public sealed record ShipmentOrderDto(
    Guid OrderId, string OrderCode, int Sequence, string PickupAddress,
    string DeliveryAddress, TransportOrderStatus Status);

public sealed record RouteStopDto(
    Guid Id, Guid ShipmentId, Guid? TransportOrderId, RouteStopType Type, int Sequence,
    string Address, decimal? Latitude, decimal? Longitude, DateTime? ArrivedAtUtc,
    DateTime? CompletedAtUtc, RouteStopStatus Status, DeliveryProofDto? DeliveryProof);

public sealed record ShipmentLocationDto(
    Guid Id, Guid ShipmentId, decimal Latitude, decimal Longitude,
    decimal? SpeedKph, decimal? AccuracyMeters, DateTime RecordedAtUtc);

public sealed record DeliveryProofDto(
    Guid Id, Guid RouteStopId, string ReceiverName, string? PhotoUrl,
    string? SignatureData, string? Note, DateTime CapturedAtUtc);

public sealed record CreateCustomerCommand(
    string Code, string Name, string Phone, string? Email,
    string? TaxCode, string? Address);

public sealed record UpdateCustomerCommand(
    string Name, string Phone, string? Email, string? TaxCode, string? Address);

public sealed record CreateDriverCommand(
    string EmployeeCode, string FullName, string Phone, string LicenseNumber,
    string LicenseClass, DateOnly LicenseExpiryDate);

public sealed record UpdateDriverCommand(
    string FullName, string Phone, string LicenseNumber,
    string LicenseClass, DateOnly LicenseExpiryDate);

public sealed record CreateVehicleCommand(
    string LicensePlate, string VehicleType, decimal MaxLoadKg,
    decimal? CargoVolumeM3, DateOnly? RegistrationExpiryDate,
    DateOnly? InsuranceExpiryDate);

public sealed record UpdateVehicleCommand(
    string VehicleType, decimal MaxLoadKg, decimal? CargoVolumeM3,
    DateOnly? RegistrationExpiryDate, DateOnly? InsuranceExpiryDate);

public sealed record CreateTransportOrderCommand(
    string Code, Guid CustomerId, string PickupAddress, string DeliveryAddress,
    string GoodsDescription, decimal WeightKg, int PackageCount,
    decimal? VolumeM3, DateTime? ExpectedPickupAtUtc,
    DateTime? ExpectedDeliveryAtUtc, decimal? EstimatedPrice,
    string? SenderName, string? SenderPhone, string? RecipientName,
    string? RecipientPhone, string? Note);

public sealed record UpdateTransportOrderCommand(
    string PickupAddress, string DeliveryAddress, string GoodsDescription,
    decimal WeightKg, int PackageCount, decimal? VolumeM3,
    DateTime? ExpectedPickupAtUtc, DateTime? ExpectedDeliveryAtUtc,
    decimal? EstimatedPrice, string? SenderName, string? SenderPhone,
    string? RecipientName, string? RecipientPhone, string? Note);

public sealed record CreateShipmentCommand(
    string Code, Guid VehicleId, Guid DriverId,
    DateTime PlannedDepartureAtUtc, IReadOnlyList<Guid> OrderIds);

public sealed record CreateCustomerOrderCommand(
    string PickupAddress, string DeliveryAddress, string GoodsDescription,
    decimal WeightKg, int PackageCount, decimal? VolumeM3,
    DateTime? ExpectedPickupAtUtc, DateTime? ExpectedDeliveryAtUtc,
    string? SenderName, string? SenderPhone, string? RecipientName,
    string? RecipientPhone, string? Note);

public sealed record UpdateShipmentLocationCommand(
    decimal Latitude, decimal Longitude, decimal? SpeedKph, decimal? AccuracyMeters);

public sealed record ArriveAtStopCommand(decimal Latitude, decimal Longitude);

public sealed record CreateDeliveryProofCommand(
    string ReceiverName, string? PhotoUrl, string? SignatureData, string? Note);
