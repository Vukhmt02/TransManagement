using TransManagement.Domain.Enums;

namespace TransManagement.API.Hubs;

public static class ChatHubEvents
{
    public const string MessageReceived = "messageReceived";
    public const string ConversationChanged = "conversationChanged";
    public const string MessagesRead = "messagesRead";
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
