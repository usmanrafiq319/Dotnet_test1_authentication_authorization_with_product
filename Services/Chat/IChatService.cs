using Dotnet_test1_authentication_authorization_with_product.Models;

namespace Dotnet_test1_authentication_authorization_with_product.Services.Chat
{
    public interface IChatService
    {
        Task HandleUserMessageAsync(
            Guid userId,
            string text,
            CancellationToken cancellationToken = default
        );

        Task HandleAdminMessageAsync(
            Guid adminId,
            Guid conversationId,
            string text,
            CancellationToken cancellationToken = default
        );

        Task MarkAdminMessagesAsReadAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default
        );

        Task MarkUserMessagesAsReadAsync(
            Guid userId,
            CancellationToken cancellationToken = default
        );
        Task ToggleAiAsync(
            Guid conversationId,
            Guid currentUserId,
            bool isAdmin,
            CancellationToken cancellationToken = default
        );

        Task<UserConversationDto?>
            GetMyConversationAsync(
                Guid userId,
                CancellationToken cancellationToken = default
            );

        Task<AdminChatSummaryDto>
            GetAdminSummaryAsync(
                CancellationToken cancellationToken = default
            );

        Task<List<ConversationSummaryDto>>
            GetAllConversationsAsync(
                CancellationToken cancellationToken = default
            );

        Task<AdminConversationDto?>
            GetConversationAsync(
                Guid conversationId,
                CancellationToken cancellationToken = default
            );
    }
}
