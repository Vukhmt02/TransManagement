using TransManagement.Domain.Common;
using TransManagement.Domain.Enums;

namespace TransManagement.Domain.Entities;

public sealed class ChatMessage : AuditableEntity
{
    private ChatMessage()
    {
    }

    public ChatMessage(
        Guid conversationId,
        Guid senderUserId,
        string content,
        DateTime sentAtUtc,
        ChatMessageType messageType = ChatMessageType.Text)
    {
        if (conversationId == Guid.Empty)
            throw new ArgumentException("Conversation ID is required.", nameof(conversationId));

        if (senderUserId == Guid.Empty)
            throw new ArgumentException("Sender user ID is required.", nameof(senderUserId));

        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        ConversationId = conversationId;
        SenderUserId = senderUserId;
        Content = content.Trim();
        SentAtUtc = sentAtUtc;
        MessageType = messageType;
    }

    public Guid ConversationId { get; private set; }

    public Conversation Conversation { get; private set; } = null!;

    public Guid SenderUserId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTime SentAtUtc { get; private set; }

    public DateTime? ReadAtUtc { get; private set; }

    public ChatMessageType MessageType { get; private set; } = ChatMessageType.Text;

    public void MarkAsRead(DateTime readAtUtc)
    {
        ReadAtUtc ??= readAtUtc;
    }
}
