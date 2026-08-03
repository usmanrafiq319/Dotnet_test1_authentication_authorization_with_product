using Dotnet_test1_authentication_authorization_with_product.Entities;

namespace Dotnet_test1_authentication_authorization_with_product.Services.Groq
{
    public interface IGroqChatService
    {
    Task<string?> GenerateReplyAsync(IReadOnlyCollection<ChatMessage> messages, CancellationToken cancellationToken = default);

    }
}
