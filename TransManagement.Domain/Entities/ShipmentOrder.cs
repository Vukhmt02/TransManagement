using TransManagement.Domain.Common;

namespace TransManagement.Domain.Entities;

public sealed class ShipmentOrder : AuditableEntity
{
    private ShipmentOrder()
    {
    }

    public ShipmentOrder(Guid shipmentId, Guid transportOrderId, int sequence)
    {
        if (shipmentId == Guid.Empty)
        {
            throw new ArgumentException("Shipment ID is required.", nameof(shipmentId));
        }

        if (transportOrderId == Guid.Empty)
        {
            throw new ArgumentException("Transport order ID is required.", nameof(transportOrderId));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequence);

        ShipmentId = shipmentId;
        TransportOrderId = transportOrderId;
        Sequence = sequence;
    }

    public Guid ShipmentId { get; private set; }

    public Shipment Shipment { get; private set; } = null!;

    public Guid TransportOrderId { get; private set; }

    public TransportOrder TransportOrder { get; private set; } = null!;

    public int Sequence { get; private set; }
}
