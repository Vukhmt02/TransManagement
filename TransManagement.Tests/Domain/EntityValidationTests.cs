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

    [Fact]
    public void RouteStop_ShouldRequireArrivalBeforeCompletion()
    {
        var stop = new RouteStop(Guid.NewGuid(), RouteStopType.Delivery, 1, "Delivery address");

        Assert.Throws<InvalidOperationException>(() => stop.Complete(DateTime.UtcNow));
        stop.Arrive(10.7769m, 106.7009m, DateTime.UtcNow);
        stop.Complete(DateTime.UtcNow.AddMinutes(5));

        Assert.Equal(RouteStopStatus.Completed, stop.Status);
    }

    [Fact]
    public void ShipmentLocation_ShouldRejectInvalidCoordinates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShipmentLocation(Guid.NewGuid(), Guid.NewGuid(), 91, 106.7m, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void DeliveryProof_ShouldRequirePhotoOrSignature()
    {
        Assert.Throws<ArgumentException>(() =>
            new DeliveryProof(Guid.NewGuid(), "Receiver", null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Conversation_ShouldFollowAssignmentAndCloseLifecycle()
    {
        var adminUserId = Guid.NewGuid();
        var conversation = new Conversation(Guid.NewGuid(), "  Support request  ");

        conversation.AssignTo(adminUserId);
        conversation.Close(DateTime.UtcNow);

        Assert.Equal("Support request", conversation.Subject);
        Assert.Equal(adminUserId, conversation.AssignedAdminUserId);
        Assert.Equal(ConversationStatus.Closed, conversation.Status);
        Assert.NotNull(conversation.ClosedAtUtc);
    }

    [Fact]
    public void Conversation_ShouldRejectAssigningAClosedConversation()
    {
        var conversation = new Conversation(Guid.NewGuid(), "Support request");
        conversation.Close(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => conversation.AssignTo(Guid.NewGuid()));
    }

    [Fact]
    public void ChatMessage_ShouldTrimContentAndMarkAsReadOnce()
    {
        var firstReadAtUtc = DateTime.UtcNow;
        var message = new ChatMessage(
            Guid.NewGuid(), Guid.NewGuid(), "  Hello  ", DateTime.UtcNow);

        message.MarkAsRead(firstReadAtUtc);
        message.MarkAsRead(firstReadAtUtc.AddMinutes(1));

        Assert.Equal("Hello", message.Content);
        Assert.Equal(ChatMessageType.Text, message.MessageType);
        Assert.Equal(firstReadAtUtc, message.ReadAtUtc);
    }
}
