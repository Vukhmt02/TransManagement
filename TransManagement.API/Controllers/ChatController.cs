using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TransManagement.API.Contracts.Common;
using TransManagement.API.Hubs;
using TransManagement.Application.Chat;
using TransManagement.Application.Common.Security;
using TransManagement.Domain.Enums;

namespace TransManagement.API.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Customer}")]
[Route("api/chat")]
public sealed class ChatController(
    IChatService chatService,
    IHubContext<ChatHub> chatHub) : ApiControllerBase
{
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(
        [FromQuery] ConversationStatus? status,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var conversations = await chatService.GetConversationsAsync(
            userId, IsAdmin, status, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ConversationDto>>.Ok(conversations));
    }

    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<IActionResult> GetConversation(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await chatService.GetConversationAsync(
            userId, IsAdmin, conversationId, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponse<ConversationDto>.Ok(result.Value))
            : ErrorResponse(result.Error);
    }

    [HttpPost("conversations")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> CreateConversation(
        CreateConversationCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await chatService.CreateConversationAsync(userId, command, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result.Error);

        await NotifyConversationChangedAsync(result.Value, "created", cancellationToken);
        return CreatedAtAction(
            nameof(GetConversation),
            new { conversationId = result.Value.Id },
            ApiResponse<ConversationDto>.Ok(result.Value));
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await chatService.GetMessagesAsync(
            userId, IsAdmin, conversationId, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponse<IReadOnlyList<ChatMessageDto>>.Ok(result.Value))
            : ErrorResponse(result.Error);
    }

    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        SendChatMessageCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await chatService.SendMessageAsync(
            userId, IsAdmin, conversationId, command, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result.Error);

        await chatHub.Clients
            .Group(ChatHub.ConversationGroupName(conversationId))
            .SendAsync(ChatHubEvents.MessageReceived, result.Value, cancellationToken);

        var conversation = await chatService.GetConversationAsync(
            userId, IsAdmin, conversationId, cancellationToken);
        if (conversation.IsSuccess)
            await NotifyConversationChangedAsync(conversation.Value, "messageSent", cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<ChatMessageDto>.Ok(result.Value));
    }

    [HttpPut("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await chatService.MarkMessagesAsReadAsync(
            userId, IsAdmin, conversationId, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result.Error);

        await chatHub.Clients
            .Group(ChatHub.ConversationGroupName(conversationId))
            .SendAsync(
                ChatHubEvents.MessagesRead,
                new MessagesReadEvent(conversationId, userId, DateTime.UtcNow),
                cancellationToken);
        return NoContent();
    }

    [HttpPut("conversations/{conversationId:guid}/assign")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Assign(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await chatService.AssignConversationAsync(
            conversationId, userId, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result.Error);

        await NotifyConversationChangedAsync(result.Value, "assigned", cancellationToken);
        return Ok(ApiResponse<ConversationDto>.Ok(result.Value));
    }

    [HttpPut("conversations/{conversationId:guid}/close")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Close(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await chatService.CloseConversationAsync(conversationId, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result.Error);

        await NotifyCurrentConversationAsync(userId, conversationId, "closed", cancellationToken);
        return NoContent();
    }

    [HttpPut("conversations/{conversationId:guid}/reopen")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Reopen(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await chatService.ReopenConversationAsync(conversationId, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result.Error);

        await NotifyCurrentConversationAsync(userId, conversationId, "reopened", cancellationToken);
        return NoContent();
    }

    private async Task NotifyCurrentConversationAsync(
        Guid adminUserId,
        Guid conversationId,
        string change,
        CancellationToken cancellationToken)
    {
        var conversation = await chatService.GetConversationAsync(
            adminUserId, true, conversationId, cancellationToken);
        if (conversation.IsSuccess)
            await NotifyConversationChangedAsync(conversation.Value, change, cancellationToken);
    }

    private Task NotifyConversationChangedAsync(
        ConversationDto conversation,
        string change,
        CancellationToken cancellationToken) =>
        chatHub.Clients
            .Groups(
                ChatHub.AdminGroupName,
                ChatHub.UserGroupName(conversation.CustomerUserId),
                ChatHub.ConversationGroupName(conversation.Id))
            .SendAsync(
                ChatHubEvents.ConversationChanged,
                new ConversationChangedEvent(
                    conversation.Id,
                    conversation.Status,
                    conversation.CustomerUserId,
                    conversation.AssignedAdminUserId,
                    change,
                    DateTime.UtcNow),
                cancellationToken);

    private bool IsAdmin => User.IsInRole(AppRoles.Admin);

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
