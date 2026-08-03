using System.Text.Json.Serialization;

namespace Dotnet_test1_authentication_authorization_with_product.Services.Groq
{
    public class GroqContracts
    {
        public sealed class GroqChatRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } =
                string.Empty;

            [JsonPropertyName("messages")]
            public List<GroqChatMessage> Messages
            {
                get;
                set;
            } = [];

            [JsonPropertyName("temperature")]
            public double Temperature { get; set; }

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; }
        }

        public sealed class GroqChatMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } =
                string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; set; } =
                string.Empty;
        }

        public sealed class GroqChatResponse
        {
            [JsonPropertyName("choices")]
            public List<GroqChoice> Choices
            {
                get;
                set;
            } = [];
        }

        public sealed class GroqChoice
        {
            [JsonPropertyName("message")]
            public GroqResponseMessage Message
            {
                get;
                set;
            } = new();
        }

        public sealed class GroqResponseMessage
        {
            [JsonPropertyName("content")]
            public string Content { get; set; } =
                string.Empty;
        }
    }
}
