using TransManagement.Application.Common.Models;
using TransManagement.Domain.Enums;

namespace TransManagement.Application.Chat;

public interface IChatService
{
    Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(
        Guid currentUserId,
        bool isAdmin,
        ConversationStatus? status,
        CancellationToken cancellationToken);

    Task<Result<ConversationDto>> GetConversationAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid conversationId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ChatMessageDto>>> GetMessagesAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid conversationId,
        CancellationToken cancellationToken);

    Task<Result<ConversationDto>> CreateConversationAsync(
        Guid customerUserId,
        CreateConversationCommand command,
        CancellationToken cancellationToken);

    Task<Result<ChatMessageDto>> SendMessageAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid conversationId,
        SendChatMessageCommand command,
        CancellationToken cancellationToken);

    Task<Result> MarkMessagesAsReadAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid conversationId,
        CancellationToken cancellationToken);

    Task<Result<ConversationDto>> AssignConversationAsync(
        Guid conversationId,
        Guid adminUserId,
        CancellationToken cancellationToken);

    Task<Result> CloseConversationAsync(Guid conversationId, CancellationToken cancellationToken);

    Task<Result> ReopenConversationAsync(Guid conversationId, CancellationToken cancellationToken);
}
