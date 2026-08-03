namespace Dotnet_test1_authentication_authorization_with_product.Entities
{
    public class ChatMessage
    {
        public Guid Id { get; set; }

        public Guid ConversationId { get; set; }

        // Can be the normal user ID or an admin ID or AI
        public Guid? SenderId { get; set; }

        public string Text { get; set; } = string.Empty;

        public ChatSenderType SenderType { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public Conversation Conversation { get; set; } = null!;
    }
}
