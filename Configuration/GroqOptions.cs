namespace Dotnet_test1_authentication_authorization_with_product.Configuration
{
    public class GroqOptions
    {
        public const string SectionName = "Groq";

        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/";

        public string Model { get; set; } ="llama-3.1-8b-instant";

        public int MaxHistoryMessages { get; set; } =20;

        public int MaxTokens { get; set; } =300;

        public double Temperature { get; set; } =0.3;
    }
}
