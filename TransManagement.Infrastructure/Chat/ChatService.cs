using Microsoft.EntityFrameworkCore;
using TransManagement.Application.Chat;
using TransManagement.Application.Common.Models;
using TransManagement.Domain.Entities;
using TransManagement.Domain.Enums;
using TransManagement.Infrastructure.Persistence;

namespace TransManagement.Infrastructure.Chat;

public sealed class ChatService(ApplicationDbContext dbContext) : IChatService
{
    private static readonly Error NotFound = new("Chat.NotFound", "The conversation was not found.");
    private static readonly Error Closed = new("Chat.Closed", "The conversation is closed.");
    private static readonly Error InvalidSubject = new("Chat.InvalidSubject", "The subject must contain between 1 and 200 characters.");
    private static readonly Error InvalidMessage = new("Chat.InvalidMessage", "The message must contain between 1 and 2000 characters.");

    public async Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(
        Guid currentUserId,
        bool isAdmin,
        ConversationStatus? status,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Messages)
            .AsQueryable();

        if (!isAdmin)
            query = query.Where(conversation => conversation.CustomerUserId == currentUserId);
        if (status.HasValue)
            query = query.Where(conversation => conversation.Status == status.Value);

        var conversations = await query
            .OrderByDescending(conversation => conversation.Messages
                .Select(message => (DateTime?)message.SentAtUtc)
                .Max() ?? conversation.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return await MapConversationsAsync(conversations, currentUserId, cancellationToken);
    }

    public async Task<Result<ConversationDto>> GetConversationAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await FindAccessibleConversationAsync(
            currentUserId, isAdmin, conversationId, true, cancellationToken);
        if (conversation is null) return Result<ConversationDto>.Failure(NotFound);

        var mapped = await MapConversationsAsync([conversation], currentUserId, cancellationToken);
        return Result<ConversationDto>.Success(mapped[0]);
    }

    public async Task<Result<IReadOnlyList<ChatMessageDto>>> GetMessagesAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var canAccess = await AccessibleConversations(currentUserId, isAdmin)
            .AnyAsync(conversation => conversation.Id == conversationId, cancellationToken);
        if (!canAccess) return Result<IReadOnlyList<ChatMessageDto>>.Failure(NotFound);

        var messages = await dbContext.ChatMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.SentAtUtc)
            .ThenBy(message => message.Id)
            .ToListAsync(cancellationToken);
        var names = await LoadUserNamesAsync(messages.Select(message => message.SenderUserId), cancellationToken);

        return Result<IReadOnlyList<ChatMessageDto>>.Success(messages
            .Select(message => Map(message, names))
            .ToList());
    }

    public async Task<Result<ConversationDto>> CreateConversationAsync(
        Guid customerUserId,
        CreateConversationCommand command,
        CancellationToken cancellationToken)
    {
        if (!IsValid(command.Subject, 200)) return Result<ConversationDto>.Failure(InvalidSubject);
        if (!IsValid(command.InitialMessage, 2000)) return Result<ConversationDto>.Failure(InvalidMessage);

        var now = DateTime.UtcNow;
        var conversation = new Conversation(customerUserId, command.Subject);
        conversation.Messages.Add(new ChatMessage(
            conversation.Id, customerUserId, command.InitialMessage, now));

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var mapped = await MapConversationsAsync([conversation], customerUserId, cancellationToken);
        return Result<ConversationDto>.Success(mapped[0]);
    }

    public async Task<Result<ChatMessageDto>> SendMessageAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid conversationId,
        SendChatMessageCommand command,
        CancellationToken cancellationToken)
    {
        if (!IsValid(command.Content, 2000)) return Result<ChatMessageDto>.Failure(InvalidMessage);

        var conversation = await FindAccessibleConversationAsync(
            currentUserId, isAdmin, conversationId, false, cancellationToken);
        if (conversation is null) return Result<ChatMessageDto>.Failure(NotFound);
        if (conversation.Status == ConversationStatus.Closed)
            return Result<ChatMessageDto>.Failure(Closed);

        if (isAdmin && !conversation.AssignedAdminUserId.HasValue)
            conversation.AssignTo(currentUserId);

        var message = new ChatMessage(
            conversationId, currentUserId, command.Content, DateTime.UtcNow);
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        var names = await LoadUserNamesAsync([currentUserId], cancellationToken);
        return Result<ChatMessageDto>.Success(Map(message, names));
    }

    public async Task<Result> MarkMessagesAsReadAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var canAccess = await AccessibleConversations(currentUserId, isAdmin)
            .AnyAsync(conversation => conversation.Id == conversationId, cancellationToken);
        if (!canAccess) return Result.Failure(NotFound);

        var unreadMessages = await dbContext.ChatMessages
            .Where(message => message.ConversationId == conversationId &&
                              message.SenderUserId != currentUserId &&
                              message.ReadAtUtc == null)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var message in unreadMessages) message.MarkAsRead(now);
        if (unreadMessages.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<ConversationDto>> AssignConversationAsync(
        Guid conversationId,
        Guid adminUserId,
        CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .Include(item => item.Messages)
            .SingleOrDefaultAsync(item => item.Id == conversationId, cancellationToken);
        if (conversation is null) return Result<ConversationDto>.Failure(NotFound);
        if (conversation.Status == ConversationStatus.Closed)
            return Result<ConversationDto>.Failure(Closed);

        conversation.AssignTo(adminUserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        var mapped = await MapConversationsAsync([conversation], adminUserId, cancellationToken);
        return Result<ConversationDto>.Success(mapped[0]);
    }

    public async Task<Result> CloseConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(item => item.Id == conversationId, cancellationToken);
        if (conversation is null) return Result.Failure(NotFound);
        if (conversation.Status == ConversationStatus.Closed) return Result.Failure(Closed);

        conversation.Close(DateTime.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ReopenConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(item => item.Id == conversationId, cancellationToken);
        if (conversation is null) return Result.Failure(NotFound);

        conversation.Reopen();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private IQueryable<Conversation> AccessibleConversations(Guid currentUserId, bool isAdmin) =>
        isAdmin
            ? dbContext.Conversations
            : dbContext.Conversations.Where(conversation => conversation.CustomerUserId == currentUserId);

    private Task<Conversation?> FindAccessibleConversationAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid conversationId,
        bool includeMessages,
        CancellationToken cancellationToken)
    {
        IQueryable<Conversation> query = AccessibleConversations(currentUserId, isAdmin);
        if (includeMessages) query = query.Include(conversation => conversation.Messages);
        return query.SingleOrDefaultAsync(conversation => conversation.Id == conversationId, cancellationToken);
    }

    private async Task<IReadOnlyList<ConversationDto>> MapConversationsAsync(
        IReadOnlyCollection<Conversation> conversations,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var userIds = conversations
            .SelectMany(conversation => new Guid?[]
            {
                conversation.CustomerUserId,
                conversation.AssignedAdminUserId
            })
            .Where(id => id.HasValue)
            .Select(id => id!.Value);
        var names = await LoadUserNamesAsync(userIds, cancellationToken);

        return conversations.Select(conversation =>
        {
            var lastMessageAtUtc = conversation.Messages.Count == 0
                ? (DateTime?)null
                : conversation.Messages.Max(message => message.SentAtUtc);
            var unreadCount = conversation.Messages.Count(message =>
                message.SenderUserId != currentUserId && !message.ReadAtUtc.HasValue);

            return new ConversationDto(
                conversation.Id,
                conversation.CustomerUserId,
                GetName(names, conversation.CustomerUserId),
                conversation.AssignedAdminUserId,
                conversation.AssignedAdminUserId.HasValue
                    ? GetName(names, conversation.AssignedAdminUserId.Value)
                    : null,
                conversation.Subject,
                conversation.Status,
                conversation.CreatedAtUtc,
                conversation.UpdatedAtUtc,
                conversation.ClosedAtUtc,
                lastMessageAtUtc,
                unreadCount);
        }).ToList();
    }

    private async Task<Dictionary<Guid, string>> LoadUserNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToArray();
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.FullName, cancellationToken);
    }

    private static ChatMessageDto Map(ChatMessage message, IReadOnlyDictionary<Guid, string> names) =>
        new(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            GetName(names, message.SenderUserId),
            message.Content,
            message.SentAtUtc,
            message.ReadAtUtc,
            message.MessageType);

    private static string GetName(IReadOnlyDictionary<Guid, string> names, Guid userId) =>
        names.TryGetValue(userId, out var name) ? name : "Unknown user";

    private static bool IsValid(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength;
}
