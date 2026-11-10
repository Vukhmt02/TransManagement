using TransManagement.Domain.Enums;

namespace TransManagement.API.Hubs;

public static class ChatHubEvents
{
    public const string MessageReceived = "messageReceived";
    public const string ConversationChanged = "conversationChanged";
    public const string MessagesRead = "messagesRead";
    public const string AiTyping = "aiTyping";
}

public sealed record ConversationChangedEvent(
    Guid ConversationId,
    ConversationStatus Status,
    Guid CustomerUserId,
    Guid? AssignedAdminUserId,
    string Change,
    DateTime OccurredAtUtc);

public sealed record MessagesReadEvent(
    Guid ConversationId,
    Guid ReadByUserId,
    DateTime ReadAtUtc);

public sealed record AiTypingEvent(
    Guid ConversationId,
    string BotName,
    bool IsTyping,
    DateTime OccurredAtUtc);
