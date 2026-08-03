using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Models;

namespace Dotnet_test1_authentication_authorization_with_product.Mappers
{
    public static class ChatMapper
    {
        public static ChatMessageDto ToDto(ChatMessage message, Guid conversationUserId){
            return new ChatMessageDto
            {
                Id = message.Id,
                ConversationId =
                    message.ConversationId,
                UserId = conversationUserId,
                SenderId = message.SenderId,
                SenderType =
                    message.SenderType.ToString(),
                Text = message.Text,
                SentAt = message.SentAt
            };
        }

        public static ConversationUpdatedDto ToConversationUpdatedDto(Conversation conversation)
        {
            return new ConversationUpdatedDto
            {
                ConversationId =
                    conversation.Id,
                UserId =
                    conversation.UserId,
                AdminUnreadCount =
                    conversation.AdminUnreadCount,
                UserUnreadCount =
                    conversation.UserUnreadCount,
                Mode =
                    conversation.Mode.ToString()
            };
        }
    }
}





