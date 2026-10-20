using TransManagement.Domain.Common;
using TransManagement.Domain.Enums;

namespace TransManagement.Domain.Entities;

public sealed class Conversation : AuditableEntity
{
    private Conversation()
    {
    }

    public Conversation(Guid customerUserId, string subject)
    {
        if (customerUserId == Guid.Empty)
            throw new ArgumentException("Customer user ID is required.", nameof(customerUserId));

        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        CustomerUserId = customerUserId;
        Subject = subject.Trim();
    }

    public Guid CustomerUserId { get; private set; }

    public Guid? AssignedAdminUserId { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public ConversationStatus Status { get; private set; } = ConversationStatus.Open;

    public DateTime? ClosedAtUtc { get; private set; }

    public ICollection<ChatMessage> Messages { get; private set; } = [];

    public void AssignTo(Guid adminUserId)
    {
        if (adminUserId == Guid.Empty)
            throw new ArgumentException("Admin user ID is required.", nameof(adminUserId));

        if (Status == ConversationStatus.Closed)
            throw new InvalidOperationException("A closed conversation cannot be assigned.");

        AssignedAdminUserId = adminUserId;
        Status = ConversationStatus.Assigned;
    }

    public void Close(DateTime closedAtUtc)
    {
        if (Status == ConversationStatus.Closed)
            throw new InvalidOperationException("The conversation is already closed.");

        Status = ConversationStatus.Closed;
        ClosedAtUtc = closedAtUtc;
    }

    public void Reopen()
    {
        Status = AssignedAdminUserId.HasValue
            ? ConversationStatus.Assigned
            : ConversationStatus.Open;
        ClosedAtUtc = null;
    }
}
