using Dotnet_test1_authentication_authorization_with_product.Data;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Hubs;
using Dotnet_test1_authentication_authorization_with_product.Mappers;
using Dotnet_test1_authentication_authorization_with_product.Models;
using Dotnet_test1_authentication_authorization_with_product.Services.Groq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Dotnet_test1_authentication_authorization_with_product.Services.Chat
{
public class ChatService : IChatService
{
    private const string AdminGroup = "admins";

    private readonly UserDbContext _dbContext;

    private readonly IGroqChatService _groqChatService;

    private readonly IHubContext<ChatHub> _hubContext;

    private readonly ILogger<ChatService> _logger;

    public ChatService(UserDbContext dbContext, IGroqChatService groqChatService, IHubContext<ChatHub> hubContext, ILogger<ChatService> logger)
    {
        _dbContext = dbContext;
        _groqChatService = groqChatService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleUserMessageAsync(Guid userId,string text,CancellationToken cancellationToken = default){
        ValidateMessage(text);

        var cleanText = text.Trim();

        var conversation = await GetOrCreateConversationAsync(userId, cancellationToken);

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            SenderId = userId,
            SenderType = ChatSenderType.User,
            Text = cleanText,
            SentAt = DateTime.UtcNow
        };

        conversation.AdminUnreadCount++;

        _dbContext.ChatMessages.Add(userMessage);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var userMessageDto = ChatMapper.ToDto(userMessage,conversation.UserId);

        /*
         * Send the user message to admins.
         */
        await _hubContext.Clients
            .Group(AdminGroup)
            .SendAsync(
                "ReceiveMessage",
                userMessageDto,
                cancellationToken
            );

        /*
         * Send it back to the user's other tabs.
         */
        await _hubContext.Clients
            .Group(
                GetUserGroup(
                    conversation.UserId
                )
            )
            .SendAsync(
                "ReceiveMessage",
                userMessageDto,
                cancellationToken
            );

        await SendConversationUpdatedAsync(
            conversation,
            cancellationToken
        );

        /*
         * Human mode means an admin has taken over.
         * Groq must not respond.
         */
        if (
            conversation.Mode ==
            ConversationMode.Human
        )
        {
            return;
        }

        await TryGenerateAiReplyAsync(
            conversation.Id,
            cancellationToken
        );
    }

    public async Task HandleAdminMessageAsync(Guid adminId,Guid conversationId,string text,CancellationToken cancellationToken = default){
        ValidateMessage(text);

        var cleanText = text.Trim();

        var conversation =
            await _dbContext.Conversations
                .FirstOrDefaultAsync(
                    conversation =>
                        conversation.Id ==
                        conversationId,
                    cancellationToken
                );

        if (conversation is null)
        {
            throw new InvalidOperationException(
                "Conversation not found."
            );
        }

        /*
         * Admin takeover:
         * once an admin replies, AI is disabled.
         */
        conversation.Mode = ConversationMode.Human;

        var adminMessage =
            new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId =
                    conversation.Id,
                SenderId = adminId,
                SenderType =
                    ChatSenderType.Admin,
                Text = cleanText,
                SentAt = DateTime.UtcNow
            };

        conversation.UserUnreadCount++;

        _dbContext.ChatMessages.Add(
            adminMessage
        );

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );

        var messageDto =
            ChatMapper.ToDto(
                adminMessage,
                conversation.UserId
            );

        /*
         * Send admin reply to user.
         */
        await _hubContext.Clients
            .Group(
                GetUserGroup(
                    conversation.UserId
                )
            )
            .SendAsync(
                "ReceiveMessage",
                messageDto,
                cancellationToken
            );

        /*
         * Send it to all admin screens as well.
         */
        await _hubContext.Clients
            .Group(AdminGroup)
            .SendAsync(
                "ReceiveMessage",
                messageDto,
                cancellationToken
            );

        await _hubContext.Clients
            .Group(
                GetUserGroup(
                    conversation.UserId
                )
            )
            .SendAsync(
                "UnreadCountUpdated",
                conversation.UserUnreadCount,
                cancellationToken
            );

        await SendConversationUpdatedAsync(
            conversation,
            cancellationToken
        );
    }

    public async Task MarkAdminMessagesAsReadAsync(Guid conversationId,CancellationToken cancellationToken = default)
    {
        var conversation =
            await _dbContext.Conversations
                .FirstOrDefaultAsync(
                    conversation =>
                        conversation.Id ==
                        conversationId,
                    cancellationToken
                );

        if (conversation is null)
        {
            throw new InvalidOperationException(
                "Conversation not found."
            );
        }

        conversation.AdminUnreadCount = 0;

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );

        await SendConversationUpdatedAsync(
            conversation,
            cancellationToken
        );
    }

    public async Task MarkUserMessagesAsReadAsync(Guid userId,CancellationToken cancellationToken = default)
    {
        var conversation =
            await _dbContext.Conversations
                .FirstOrDefaultAsync(
                    conversation =>
                        conversation.UserId ==
                        userId,
                    cancellationToken
                );

        if (conversation is null)
        {
            return;
        }

        conversation.UserUnreadCount = 0;

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );

        await _hubContext.Clients
            .Group(GetUserGroup(userId))
            .SendAsync(
                "UnreadCountUpdated",
                0,
                cancellationToken
            );

        await SendConversationUpdatedAsync(
            conversation,
            cancellationToken
        );
    }

    public async Task ToggleAiAsync(Guid conversationId,Guid currentUserId,bool isAdmin,CancellationToken cancellationToken = default)
    {
        var conversation =await _dbContext.Conversations.FirstOrDefaultAsync(conversation => conversation.Id ==conversationId, cancellationToken);

        if (conversation is null)
        {
            if (isAdmin)
            {
                throw new InvalidOperationException("cnversion didnt find");
            }

            conversation = new Conversation
            {
                UserId = currentUserId,
                CreatedAt = DateTime.UtcNow,
                AdminUnreadCount = 0,
                UserUnreadCount = 0,
                Mode = ConversationMode.Human
            };

            _dbContext.Conversations.Add(
                conversation
            );

            await _dbContext.SaveChangesAsync(cancellationToken);
            await SendConversationUpdatedAsync(conversation, cancellationToken);
            return; 
           
        }

            /*
                * Admins can toggle any conversation.
                * Normal users can toggle only their own.
                */
            if (
            !isAdmin &&
            conversation.UserId != currentUserId
        )
        {
            throw new UnauthorizedAccessException(
                "You cannot change another user's conversation."
            );
        }

        conversation.Mode =
            conversation.Mode ==
            ConversationMode.Ai
                ? ConversationMode.Human
                : ConversationMode.Ai;

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );

        await SendConversationUpdatedAsync(
            conversation,
            cancellationToken
        );
    }
    public async Task<UserConversationDto?> GetMyConversationAsync(Guid userId,CancellationToken cancellationToken = default){
    return await _dbContext.Conversations
    .AsNoTracking()
    .Where(conversation =>
        conversation.UserId == userId
    )
    .Select(conversation =>
        new UserConversationDto
        {
            Id = conversation.Id,
            UserId =
                conversation.UserId,
            CreatedAt =
                conversation.CreatedAt,
            UserUnreadCount =
                conversation.UserUnreadCount,
            Mode =
                conversation.Mode.ToString(),

            Messages =
                conversation.Messages
                    .OrderBy(message =>
                        message.SentAt
                    )
                    .Select(message =>
                        new ChatMessageDto
                        {
                            Id =
                                message.Id,

                            ConversationId =
                                message
                                    .ConversationId,

                            UserId =
                                conversation.UserId,

                            SenderId =
                                message.SenderId,

                            SenderType =
                                message
                                    .SenderType
                                    .ToString(),

                            Text =
                                message.Text,

                            SentAt =
                                message.SentAt
                        }
                    )
                    .ToList()
        }
    )
    .FirstOrDefaultAsync(
        cancellationToken
    );
}

    public async Task<AdminChatSummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var unreadMessages =
            await _dbContext.Conversations
                .SumAsync(
                    conversation =>
                        conversation
                            .AdminUnreadCount,
                    cancellationToken
                );

        var unreadConversations =
            await _dbContext.Conversations
                .CountAsync(
                    conversation =>
                        conversation
                            .AdminUnreadCount > 0,
                    cancellationToken
                );

        return new AdminChatSummaryDto
        {
            UnreadMessages =
                unreadMessages,
            UnreadConversations =
                unreadConversations
        };
    }

    public async Task<List<ConversationSummaryDto>> GetAllConversationsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .AsNoTracking()
            .Select(conversation =>
                new ConversationSummaryDto
                {
                    Id = conversation.Id,
                    UserId =
                        conversation.UserId,
                    UserName =
                        conversation.User.UserName,
                    CreatedAt =
                        conversation.CreatedAt,
                    AdminUnreadCount =
                        conversation.AdminUnreadCount,
                    UserUnreadCount =
                        conversation.UserUnreadCount,
                    Mode =
                        conversation.Mode.ToString(),

                    LastMessage =
                        conversation.Messages
                            .OrderByDescending(
                                message =>
                                    message.SentAt
                            )
                            .Select(message =>
                                message.Text
                            )
                            .FirstOrDefault(),

                    LastMessageAt =
                        conversation.Messages
                            .OrderByDescending(
                                message =>
                                    message.SentAt
                            )
                            .Select(message =>
                                (DateTime?)
                                    message.SentAt
                            )
                            .FirstOrDefault()
                }
            )
            .OrderByDescending(conversation =>
                conversation.LastMessageAt
            )
            .ToListAsync(
                cancellationToken
            );
    }

    public async Task<AdminConversationDto?> GetConversationAsync(Guid conversationId,CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .AsNoTracking()
            .Where(conversation =>
                conversation.Id ==
                conversationId
            )
            .Select(conversation =>
                new AdminConversationDto
                {
                    Id = conversation.Id,
                    UserId =
                        conversation.UserId,
                    UserName =
                        conversation.User.UserName,
                    CreatedAt =
                        conversation.CreatedAt,
                    AdminUnreadCount =
                        conversation.AdminUnreadCount,
                    UserUnreadCount =
                        conversation.UserUnreadCount,
                    Mode =
                        conversation.Mode.ToString(),

                    Messages =
                        conversation.Messages
                            .OrderBy(message =>
                                message.SentAt
                            )
                            .Select(message =>
                                new ChatMessageDto
                                {
                                    Id =
                                        message.Id,

                                    ConversationId =
                                        message
                                            .ConversationId,

                                    UserId =
                                        conversation.UserId,

                                    SenderId =
                                        message.SenderId,

                                    SenderType =
                                        message
                                            .SenderType
                                            .ToString(),

                                    Text =
                                        message.Text,

                                    SentAt =
                                        message.SentAt
                                }
                            )
                            .ToList()
                }
            )
            .FirstOrDefaultAsync(
                cancellationToken
            );
    }

    private async Task TryGenerateAiReplyAsync(Guid conversationId,CancellationToken cancellationToken){
        try
        {
            /*
             * Load recent conversation history.
             */
            var recentMessages =
                await _dbContext.ChatMessages
                    .AsNoTracking()
                    .Where(message =>
                        message.ConversationId ==
                        conversationId
                    )
                    .OrderByDescending(message =>
                        message.SentAt
                    )
                    .Take(20)
                    .OrderBy(message =>
                        message.SentAt
                    )
                    .ToListAsync(
                        cancellationToken
                    );

            var aiReply =
                await _groqChatService
                    .GenerateReplyAsync(
                        recentMessages,
                        cancellationToken
                    );

            if (
                string.IsNullOrWhiteSpace(
                    aiReply
                )
            )
            {
                _logger.LogWarning(
                    "Groq returned no response for conversation {ConversationId}.",
                    conversationId
                );

                return;
            }

            /*
             * Reload the conversation after the Groq
             * request. An admin may have replied while
             * Groq was generating the answer.
             */
            var conversation =
                await _dbContext.Conversations
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            conversationId,
                        cancellationToken
                    );

            if (conversation is null)
            {
                return;
            }

            /*
             * Do not save the AI reply if an admin
             * already took over.
             */
            if (
                conversation.Mode ==
                ConversationMode.Human
            )
            {
                return;
            }

            var aiMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId =
                    conversation.Id,
                SenderId = null,
                SenderType =
                    ChatSenderType.Ai,
                Text = aiReply.Trim(),
                SentAt = DateTime.UtcNow
            };

            conversation.UserUnreadCount++;

            _dbContext.ChatMessages.Add(
                aiMessage
            );

            await _dbContext.SaveChangesAsync(
                cancellationToken
            );

            var aiMessageDto =
                ChatMapper.ToDto(
                    aiMessage,
                    conversation.UserId
                );

            /*
             * Send AI response to user.
             */
            await _hubContext.Clients
                .Group(
                    GetUserGroup(
                        conversation.UserId
                    )
                )
                .SendAsync(
                    "ReceiveMessage",
                    aiMessageDto,
                    cancellationToken
                );

            /*
             * Also send AI response to the admin
             * conversation currently open.
             */
            await _hubContext.Clients
                .Group(AdminGroup)
                .SendAsync(
                    "ReceiveMessage",
                    aiMessageDto,
                    cancellationToken
                );

            await _hubContext.Clients
                .Group(
                    GetUserGroup(
                        conversation.UserId
                    )
                )
                .SendAsync(
                    "UnreadCountUpdated",
                    conversation.UserUnreadCount,
                    cancellationToken
                );

            await SendConversationUpdatedAsync(
                conversation,
                cancellationToken
            );
        }
        catch (
            OperationCanceledException
        )
        {
            _logger.LogWarning(
                "AI generation was cancelled for conversation {ConversationId}.",
                conversationId
            );
        }
        catch (Exception exception)
        {
            /*
             * Groq failure must not remove or undo
             * the user's already-saved message.
             */
            _logger.LogError(
                exception,
                "Could not generate AI response for conversation {ConversationId}.",
                conversationId
            );
        }
    }

    private async Task<Conversation> GetOrCreateConversationAsync(Guid userId,CancellationToken cancellationToken)
    {
        var conversation =
            await _dbContext.Conversations
                .FirstOrDefaultAsync(
                    conversation =>
                        conversation.UserId ==
                        userId,
                    cancellationToken
                );

        if (conversation is not null)
        {
            return conversation;
        }

        conversation = new Conversation
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            AdminUnreadCount = 0,
            UserUnreadCount = 0,
            Mode = ConversationMode.Ai
        };

        _dbContext.Conversations.Add(
            conversation
        );

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );

        return conversation;
    }

    private async Task SendConversationUpdatedAsync(Conversation conversation,CancellationToken cancellationToken)
    {
        var update = ChatMapper.ToConversationUpdatedDto( conversation );

        await _hubContext.Clients
            .Group(AdminGroup)
            .SendAsync(
                "ConversationUpdated",
                update,
                cancellationToken
            );

        await _hubContext.Clients
            .Group(
                GetUserGroup(
                    conversation.UserId
                )
            )
            .SendAsync(
                "ConversationUpdated",
                update,
                cancellationToken
            );
    }

    private static string GetUserGroup(Guid userId)
    {
        return $"chat-user-{userId}";
    }

    private static void ValidateMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "Message cannot be empty.",
                nameof(text)
            );
        }

        if (text.Trim().Length > 2000)
        {
            throw new ArgumentException(
                "Message cannot exceed 2000 characters.",
                nameof(text)
            );
        }
    }
}

}