using TransManagement.Application.Common.Models;

namespace TransManagement.Application.Chat;

public interface IAiChatAssistant
{
    Task<Result<ChatMessageDto>> GenerateCustomerReplyAsync(
        Guid customerUserId,
        Guid conversationId,
        CancellationToken cancellationToken);
}

