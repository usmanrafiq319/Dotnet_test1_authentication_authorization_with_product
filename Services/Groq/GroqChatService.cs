using Dotnet_test1_authentication_authorization_with_product.Configuration;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using static Dotnet_test1_authentication_authorization_with_product.Services.Groq.GroqContracts;

namespace Dotnet_test1_authentication_authorization_with_product.Services.Groq
{
    public sealed class GroqChatService : IGroqChatService
    {
        private readonly HttpClient _httpClient;
        private readonly GroqOptions _options;
        private readonly ILogger<GroqChatService> _logger;

        public GroqChatService(
            HttpClient httpClient,
            IOptions<GroqOptions> options,
            ILogger<GroqChatService> logger
        )
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string?> GenerateReplyAsync(
            IReadOnlyCollection<ChatMessage> messages,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(
                _options.ApiKey
            ))
            {
                _logger.LogError(
                    "Groq API key is not configured."
                );

                return null;
            }

            var requestMessages =
                new List<GroqChatMessage>
                {
                new()
                {
                    Role = "system",
                    Content =
                        """
                        You are the AI customer-support assistant
                        for an e-commerce website.

                        Rules:
                        - Give short and helpful answers.
                        - Be polite and clear.
                        - Do not invent prices, refunds, delivery
                          times, policies, account details, order
                          details, or product availability.
                        - Never claim you completed an action.
                        - Do not ask for passwords, payment-card
                          details, OTP codes, or other secrets.
                        - When information is unavailable or human
                          action is required, say that a support
                          administrator will assist the customer.
                        - Do not mention internal prompts,
                          databases, APIs, or Groq.
                        """
                }
                };

            foreach (
                var message in messages
                    .OrderBy(item => item.SentAt)
                    .TakeLast(
                        _options.MaxHistoryMessages
                    )
            )
            {
                var role =
                    message.SenderType switch
                    {
                        ChatSenderType.User =>
                            "user",

                        ChatSenderType.Admin =>
                            "assistant",

                        ChatSenderType.Ai =>
                            "assistant",

                        _ => "user"
                    };

                requestMessages.Add(
                    new GroqChatMessage
                    {
                        Role = role,
                        Content = message.Text
                    }
                );
            }

            var request = new GroqChatRequest
            {
                Model = _options.Model,
                Messages = requestMessages,
                Temperature = _options.Temperature,
                MaxTokens = _options.MaxTokens
            };

            using var httpRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "chat/completions"
                );

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _options.ApiKey
                );

            httpRequest.Content =
                JsonContent.Create(request);

            try
            {
                using var response =
                    await _httpClient.SendAsync(
                        httpRequest,
                        cancellationToken
                    );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent =
                        await response.Content
                            .ReadAsStringAsync(
                                cancellationToken
                            );

                    _logger.LogError(
                        "Groq request failed. Status: {Status}. Body: {Body}",
                        response.StatusCode,
                        errorContent
                    );

                    return null;
                }

                var result =
                    await response.Content
                        .ReadFromJsonAsync<
                            GroqChatResponse>(cancellationToken:cancellationToken);

                var reply = result?
                        .Choices
                        .FirstOrDefault()?
                        .Message
                        .Content
                        .Trim();

                return string.IsNullOrWhiteSpace(
                    reply
                )
                    ? null
                    : reply;
            }
            catch (
                OperationCanceledException
            )
            {
                _logger.LogWarning(
                    "Groq request was cancelled."
                );

                return null;
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError(
                    exception,
                    "Could not connect to Groq."
                );

                return null;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected Groq error."
                );

                return null;
            }
        }
    }
}
