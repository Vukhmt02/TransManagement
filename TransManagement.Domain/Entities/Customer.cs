using TransManagement.Domain.Common;

namespace TransManagement.Domain.Entities;

public sealed class Customer : AuditableEntity
{
    private Customer()
    {
    }

    public Customer(string code, string name, string phone, string? email = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        Code = code.Trim();
        Name = name.Trim();
        Phone = phone.Trim();
        Email = email?.Trim();
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? TaxCode { get; private set; }

    public string? Address { get; private set; }

    public bool IsActive { get; private set; } = true;

    public ICollection<TransportOrder> TransportOrders { get; private set; } = [];

    public void Update(string name, string phone, string? email, string? taxCode, string? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        Name = name.Trim();
        Phone = phone.Trim();
        Email = email?.Trim();
        TaxCode = taxCode?.Trim();
        Address = address?.Trim();
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
