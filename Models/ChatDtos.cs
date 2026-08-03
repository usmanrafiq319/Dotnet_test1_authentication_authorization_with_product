namespace Dotnet_test1_authentication_authorization_with_product.Models
{

    public class UserConversationDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public int UserUnreadCount { get; set; }

        public string Mode { get; set; } =
            string.Empty;

        public List<ChatMessageDto> Messages { get; set; } = [];
    }

    public class AdminConversationDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string UserName { get; set; } =
            string.Empty;

        public DateTime CreatedAt { get; set; }

        public int AdminUnreadCount { get; set; }

        public int UserUnreadCount { get; set; }

        public string Mode { get; set; } =
            string.Empty;

        public List<ChatMessageDto> Messages { get; set; } = [];
    }

    public class ConversationSummaryDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string UserName { get; set; } =string.Empty;

        public DateTime CreatedAt { get; set; }

        public int AdminUnreadCount { get; set; }

        public int UserUnreadCount { get; set; }

        public string Mode { get; set; } =
            string.Empty;

        public string? LastMessage { get; set; }

        public DateTime? LastMessageAt { get; set; }
    }

    public class AdminChatSummaryDto
    {
        public int UnreadMessages { get; set; }

        public int UnreadConversations { get; set; }
    }

    public class ConversationUpdatedDto
    {
        public Guid ConversationId { get; set; }

        public Guid UserId { get; set; }

        public int AdminUnreadCount { get; set; }

        public int UserUnreadCount { get; set; }

        public string Mode { get; set; } = string.Empty;
    }
}
