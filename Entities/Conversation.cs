namespace Dotnet_test1_authentication_authorization_with_product.Entities
{
    public class Conversation
    {
        public Guid Id { get; set; }

        // The normal user who owns this support conversation
        public Guid UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // New user messages that admins have not opened
        public int AdminUnreadCount { get; set; }

        // New admin messages that the user has not opened
        public int UserUnreadCount { get; set; }

        public User User { get; set; } = null!;
        public ConversationMode Mode { get; set; } = ConversationMode.Ai;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
