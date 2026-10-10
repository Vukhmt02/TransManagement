using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TransManagement.Application.Common.Security;
using TransManagement.Infrastructure.Persistence;

namespace TransManagement.API.Hubs;

[Authorize]
public sealed class TrackingHub(ApplicationDbContext dbContext) : Hub
{
    public async Task JoinShipment(Guid shipmentId)
    {
        if (!Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new HubException("Invalid account.");
        var isManager = Context.User!.IsInRole(AppRoles.Admin) || Context.User.IsInRole(AppRoles.Dispatcher);
        var canView = isManager || await dbContext.Shipments.AnyAsync(x => x.Id == shipmentId &&
            (x.Driver.UserId == userId || x.ShipmentOrders.Any(o => o.TransportOrder.Customer.UserId == userId)));
        if (!canView) throw new HubException("You cannot track this shipment.");
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(shipmentId));
    }

    public Task LeaveShipment(Guid shipmentId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(shipmentId));

    public static string GroupName(Guid shipmentId) => $"shipment-{shipmentId:N}";
}
