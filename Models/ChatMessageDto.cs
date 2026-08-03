namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class ChatMessageDto
    {
        public Guid Id { get; set; }

        public Guid ConversationId { get; set; }

        public Guid UserId { get; set; }

        public Guid? SenderId { get; set; }

        public string Text { get; set; } = string.Empty;

        public string SenderType { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }
    }
}
