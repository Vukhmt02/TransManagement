using TransManagement.Domain.Entities;
using TransManagement.Domain.Enums;

namespace TransManagement.Tests.Domain;

public sealed class EntityValidationTests
{
    [Fact]
    public void Vehicle_ShouldNormalizeLicensePlate()
    {
        var vehicle = new Vehicle(" 51c-123.45 ", "Truck", 3_500);

        Assert.Equal("51C-123.45", vehicle.LicensePlate);
    }

    [Fact]
    public void Vehicle_ShouldRejectNonPositiveLoad()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Vehicle("51C-123.45", "Truck", 0));
    }

    [Fact]
    public void TransportOrder_ShouldRejectMissingCustomer()
    {
        Assert.Throws<ArgumentException>(() =>
            new TransportOrder(
                "ORD-001",
                Guid.Empty,
                "Pickup address",
                "Delivery address",
                "General goods",
                100,
                1));
    }

    [Fact]
    public void TransportOrder_ShouldFollowAssignmentLifecycle()
    {
        var order = new TransportOrder(
            "ORD-001", Guid.NewGuid(), "Pickup", "Delivery", "Goods", 100, 1);

        order.ChangeStatus(TransportOrderStatus.WaitingForAssignment);
        order.ChangeStatus(TransportOrderStatus.Assigned);
        order.ChangeStatus(TransportOrderStatus.PickingUp);
        order.ChangeStatus(TransportOrderStatus.InTransit);
        order.ChangeStatus(TransportOrderStatus.Delivered);
        order.ChangeStatus(TransportOrderStatus.Completed);

        Assert.Equal(TransportOrderStatus.Completed, order.Status);
    }

    [Fact]
    public void TransportOrder_ShouldRejectInvalidStatusTransition()
    {
        var order = new TransportOrder(
            "ORD-002", Guid.NewGuid(), "Pickup", "Delivery", "Goods", 100, 1);

        Assert.Throws<InvalidOperationException>(() =>
            order.ChangeStatus(TransportOrderStatus.Completed));
    }

    [Fact]
    public void Shipment_ShouldFollowAssignedStartedCompletedLifecycle()
    {
        var shipment = new Shipment(
            "SHP-001", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        shipment.Assign();
        shipment.Start(DateTime.UtcNow);
        shipment.Complete(DateTime.UtcNow.AddHours(2));

        Assert.Equal(ShipmentStatus.Completed, shipment.Status);
        Assert.NotNull(shipment.StartedAtUtc);
        Assert.NotNull(shipment.CompletedAtUtc);
    }

    [Fact]
    public void Customer_ShouldLinkToOnlyOneUser()
    {
        var customer = new Customer("CUS-100", "Customer", "0900000000");
        var userId = Guid.NewGuid();

        customer.LinkToUser(userId);

        Assert.Equal(userId, customer.UserId);
        Assert.Throws<InvalidOperationException>(() => customer.LinkToUser(Guid.NewGuid()));
    }
}
