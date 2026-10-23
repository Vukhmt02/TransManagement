using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TransManagement.Application.Common.Security;
using TransManagement.Infrastructure.Persistence;

namespace TransManagement.API.Hubs;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Customer}")]
public sealed class ChatHub(ApplicationDbContext dbContext) : Hub
{
    public const string AdminGroupName = "chat-admins";

    public override async Task OnConnectedAsync()
    {
        if (!TryGetUserId(out var userId))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(userId));
        if (Context.User!.IsInRole(AppRoles.Admin))
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroupName);

        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId)
    {
        if (!TryGetUserId(out var userId))
            throw new HubException("Invalid account.");

        var isAdmin = Context.User!.IsInRole(AppRoles.Admin);
        var canAccess = await dbContext.Conversations
            .AsNoTracking()
            .AnyAsync(
                conversation => conversation.Id == conversationId &&
                                (isAdmin || conversation.CustomerUserId == userId),
                Context.ConnectionAborted);

        if (!canAccess)
            throw new HubException("You cannot access this conversation.");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            ConversationGroupName(conversationId),
            Context.ConnectionAborted);
    }

    public Task LeaveConversation(Guid conversationId) =>
        Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            ConversationGroupName(conversationId),
            Context.ConnectionAborted);

    public static string ConversationGroupName(Guid conversationId) =>
        $"chat-conversation-{conversationId:N}";

    public static string UserGroupName(Guid userId) => $"chat-user-{userId:N}";

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
