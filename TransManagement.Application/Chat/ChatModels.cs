using TransManagement.Domain.Enums;

namespace TransManagement.Application.Chat;

public sealed record ConversationDto(
    Guid Id,
    Guid CustomerUserId,
    string CustomerName,
    Guid? AssignedAdminUserId,
    string? AssignedAdminName,
    string Subject,
    ConversationStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ClosedAtUtc,
    DateTime? LastMessageAtUtc,
    int UnreadCount);

public sealed record ChatMessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderName,
    string Content,
    DateTime SentAtUtc,
    DateTime? ReadAtUtc,
    ChatMessageType MessageType);

public sealed record CreateConversationCommand(string Subject, string InitialMessage);

public sealed record SendChatMessageCommand(string Content);
