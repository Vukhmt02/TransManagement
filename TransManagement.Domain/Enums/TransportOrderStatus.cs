namespace TransManagement.Domain.Enums;

public enum TransportOrderStatus
{
    Draft = 1,
    WaitingForAssignment = 2,
    Assigned = 3,
    PickingUp = 4,
    InTransit = 5,
    Delivered = 6,
    Completed = 7,
    Cancelled = 8,
    DeliveryFailed = 9
}

