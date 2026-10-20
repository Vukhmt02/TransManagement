using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransManagement.API.Contracts.Common;
using TransManagement.Application.Chat;
using TransManagement.Application.Common.Security;
using TransManagement.Domain.Enums;

namespace TransManagement.API.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Customer}")]
[Route("api/chat")]
public sealed class ChatController(IChatService chatService) : ApiControllerBase
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
        return result.IsSuccess
            ? CreatedAtAction(
                nameof(GetConversation),
                new { conversationId = result.Value.Id },
                ApiResponse<ConversationDto>.Ok(result.Value))
            : ErrorResponse(result.Error);
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
        return result.IsSuccess
            ? StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<ChatMessageDto>.Ok(result.Value))
            : ErrorResponse(result.Error);
    }

    [HttpPut("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await chatService.MarkMessagesAsReadAsync(
            userId, IsAdmin, conversationId, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
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
        return result.IsSuccess
            ? Ok(ApiResponse<ConversationDto>.Ok(result.Value))
            : ErrorResponse(result.Error);
    }

    [HttpPut("conversations/{conversationId:guid}/close")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Close(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var result = await chatService.CloseConversationAsync(conversationId, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
    }

    [HttpPut("conversations/{conversationId:guid}/reopen")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Reopen(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var result = await chatService.ReopenConversationAsync(conversationId, cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResponse(result.Error);
    }

    private bool IsAdmin => User.IsInRole(AppRoles.Admin);

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
