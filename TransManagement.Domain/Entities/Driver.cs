using TransManagement.Domain.Common;
using TransManagement.Domain.Enums;

namespace TransManagement.Domain.Entities;

public sealed class Driver : AuditableEntity
{
    private Driver()
    {
    }

    public Driver(
        string employeeCode,
        string fullName,
        string phone,
        string licenseNumber,
        string licenseClass,
        DateOnly licenseExpiryDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseClass);

        EmployeeCode = employeeCode.Trim();
        FullName = fullName.Trim();
        Phone = phone.Trim();
        LicenseNumber = licenseNumber.Trim();
        LicenseClass = licenseClass.Trim();
        LicenseExpiryDate = licenseExpiryDate;
    }

    public string EmployeeCode { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string LicenseNumber { get; private set; } = string.Empty;

    public string LicenseClass { get; private set; } = string.Empty;

    public DateOnly LicenseExpiryDate { get; private set; }

    public DriverStatus Status { get; private set; } = DriverStatus.Available;

    public ICollection<Shipment> Shipments { get; private set; } = [];

    public void Update(
        string fullName,
        string phone,
        string licenseNumber,
        string licenseClass,
        DateOnly licenseExpiryDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseClass);

        FullName = fullName.Trim();
        Phone = phone.Trim();
        LicenseNumber = licenseNumber.Trim();
        LicenseClass = licenseClass.Trim();
        LicenseExpiryDate = licenseExpiryDate;
    }

    public void SetStatus(DriverStatus status) => Status = status;
}
