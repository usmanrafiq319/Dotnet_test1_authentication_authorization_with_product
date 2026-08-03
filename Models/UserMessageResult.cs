namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class UserMessageResult
    {
        public ChatMessageDto UserMessage { get; set; } = null!;

        public ChatMessageDto? AiMessage { get; set; }

        public ConversationUpdatedDto ConversationUpdate { get; set; } = null!;

        public int UserUnreadCount { get; set; }

    }
}
