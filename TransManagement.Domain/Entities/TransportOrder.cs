using TransManagement.Domain.Common;
using TransManagement.Domain.Enums;

namespace TransManagement.Domain.Entities;

public sealed class TransportOrder : AuditableEntity
{
    private TransportOrder()
    {
    }

    public TransportOrder(
        string code,
        Guid customerId,
        string pickupAddress,
        string deliveryAddress,
        string goodsDescription,
        decimal weightKg,
        int packageCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(pickupAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(goodsDescription);

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer ID is required.", nameof(customerId));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weightKg);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(packageCount);

        Code = code.Trim();
        CustomerId = customerId;
        PickupAddress = pickupAddress.Trim();
        DeliveryAddress = deliveryAddress.Trim();
        GoodsDescription = goodsDescription.Trim();
        WeightKg = weightKg;
        PackageCount = packageCount;
    }

    public string Code { get; private set; } = string.Empty;

    public Guid CustomerId { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public string PickupAddress { get; private set; } = string.Empty;

    public string DeliveryAddress { get; private set; } = string.Empty;

    public string? SenderName { get; private set; }

    public string? SenderPhone { get; private set; }

    public string? RecipientName { get; private set; }

    public string? RecipientPhone { get; private set; }

    public string GoodsDescription { get; private set; } = string.Empty;

    public decimal WeightKg { get; private set; }

    public decimal? VolumeM3 { get; private set; }

    public int PackageCount { get; private set; }

    public DateTime? ExpectedPickupAtUtc { get; private set; }

    public DateTime? ExpectedDeliveryAtUtc { get; private set; }

    public decimal? EstimatedPrice { get; private set; }

    public string? Note { get; private set; }

    public TransportOrderStatus Status { get; private set; } = TransportOrderStatus.Draft;

    public ICollection<ShipmentOrder> ShipmentOrders { get; private set; } = [];

    public ICollection<RouteStop> RouteStops { get; private set; } = [];

    public void UpdateDetails(
        string pickupAddress,
        string deliveryAddress,
        string goodsDescription,
        decimal weightKg,
        int packageCount,
        decimal? volumeM3,
        DateTime? expectedPickupAtUtc,
        DateTime? expectedDeliveryAtUtc,
        decimal? estimatedPrice,
        string? senderName,
        string? senderPhone,
        string? recipientName,
        string? recipientPhone,
        string? note)
    {
        if (Status is not TransportOrderStatus.Draft and not TransportOrderStatus.WaitingForAssignment)
        {
            throw new InvalidOperationException("Only draft or waiting orders can be edited.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(pickupAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(goodsDescription);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weightKg);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(packageCount);
        if (volumeM3 is <= 0) throw new ArgumentOutOfRangeException(nameof(volumeM3));
        if (estimatedPrice is < 0) throw new ArgumentOutOfRangeException(nameof(estimatedPrice));
        if (expectedDeliveryAtUtc < expectedPickupAtUtc)
        {
            throw new ArgumentException("Expected delivery must not be earlier than pickup.");
        }

        PickupAddress = pickupAddress.Trim();
        DeliveryAddress = deliveryAddress.Trim();
        GoodsDescription = goodsDescription.Trim();
        WeightKg = weightKg;
        PackageCount = packageCount;
        VolumeM3 = volumeM3;
        ExpectedPickupAtUtc = expectedPickupAtUtc;
        ExpectedDeliveryAtUtc = expectedDeliveryAtUtc;
        EstimatedPrice = estimatedPrice;
        SenderName = senderName?.Trim();
        SenderPhone = senderPhone?.Trim();
        RecipientName = recipientName?.Trim();
        RecipientPhone = recipientPhone?.Trim();
        Note = note?.Trim();
    }

    public void ChangeStatus(TransportOrderStatus newStatus)
    {
        var isAllowed = (Status, newStatus) switch
        {
            (TransportOrderStatus.Draft, TransportOrderStatus.WaitingForAssignment) => true,
            (TransportOrderStatus.Draft, TransportOrderStatus.Cancelled) => true,
            (TransportOrderStatus.WaitingForAssignment, TransportOrderStatus.Assigned) => true,
            (TransportOrderStatus.WaitingForAssignment, TransportOrderStatus.Cancelled) => true,
            (TransportOrderStatus.Assigned, TransportOrderStatus.WaitingForAssignment) => true,
            (TransportOrderStatus.Assigned, TransportOrderStatus.PickingUp) => true,
            (TransportOrderStatus.Assigned, TransportOrderStatus.Cancelled) => true,
            (TransportOrderStatus.PickingUp, TransportOrderStatus.InTransit) => true,
            (TransportOrderStatus.InTransit, TransportOrderStatus.Delivered) => true,
            (TransportOrderStatus.InTransit, TransportOrderStatus.DeliveryFailed) => true,
            (TransportOrderStatus.Delivered, TransportOrderStatus.Completed) => true,
            (TransportOrderStatus.DeliveryFailed, TransportOrderStatus.WaitingForAssignment) => true,
            _ => false
        };

        if (!isAllowed)
        {
            throw new InvalidOperationException($"Cannot change order status from {Status} to {newStatus}.");
        }

        Status = newStatus;
    }
}
