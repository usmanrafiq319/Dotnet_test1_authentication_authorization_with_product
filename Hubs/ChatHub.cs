using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Dotnet_test1_authentication_authorization_with_product.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private const string AdminGroup = "admins";

    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public override async Task OnConnectedAsync(){
        var userId = GetCurrentUserId();

        if (IsCurrentUserAdmin())
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                AdminGroup
            );
        }
        else
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                GetUserGroup(userId)
            );
        }

        await base.OnConnectedAsync();
    }

    public async Task SendMessageToAdmins(string text){
        if (IsCurrentUserAdmin())
        {
            throw new HubException(
                "Admins cannot use this method."
            );
        }

        try
        {
            await _chatService
                .HandleUserMessageAsync(
                    GetCurrentUserId(),
                    text,
                    Context.ConnectionAborted
                );
        }
        catch (
            ArgumentException exception
        )
        {
            throw new HubException(
                exception.Message
            );
        }
    }

    public async Task SendMessageToUser(Guid conversationId, string text){
        if (!IsCurrentUserAdmin())
        {
            throw new HubException(
                "Only admins can reply to users."
            );
        }

        try
        {
            await _chatService
                .HandleAdminMessageAsync(
                    GetCurrentUserId(),
                    conversationId,
                    text,
                    Context.ConnectionAborted
                );
        }
        catch (
            ArgumentException exception
        )
        {
            throw new HubException(
                exception.Message
            );
        }
        catch (
            InvalidOperationException exception
        )
        {
            throw new HubException(
                exception.Message
            );
        }
    }

    public async Task MarkAdminMessagesAsRead(Guid conversationId){
        if (!IsCurrentUserAdmin())
        {
            throw new HubException(
                "Only admins can use this method."
            );
        }

        try
        {
            await _chatService
                .MarkAdminMessagesAsReadAsync(
                    conversationId,
                    Context.ConnectionAborted
                );
        }
        catch (
            InvalidOperationException exception
        )
        {
            throw new HubException(
                exception.Message
            );
        }
    }

    public async Task MarkUserMessagesAsRead(){
        if (IsCurrentUserAdmin())
        {
            throw new HubException(
                "Admins cannot use this method."
            );
        }

        await _chatService
            .MarkUserMessagesAsReadAsync(
                GetCurrentUserId(),
                Context.ConnectionAborted
            );
    }

    /*
     * Admin can later call this method from a
     * button to return a conversation to AI mode.
     */
    public async Task ToggleAiForConversation(Guid conversationId)
    {
        var currentUserId = GetCurrentUserId();

        var isAdmin = IsCurrentUserAdmin();

        try
        {
            await _chatService.ToggleAiAsync(
                conversationId,
                currentUserId,
                isAdmin,
                Context.ConnectionAborted
            );
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(
                exception.Message
            );
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new HubException(
                exception.Message
            );
        }
    }
    private Guid GetCurrentUserId()
    {
        var value =
            Context.User?
                .FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

        if (
            !Guid.TryParse(
                value,
                out var userId
            )
        )
        {
            throw new HubException(
                "Authenticated user ID is invalid."
            );
        }

        return userId;
    }

    private bool IsCurrentUserAdmin()
    {
        return Context.User?
            .IsInRole(
                UserRole.Admin.ToString()
            ) == true;
    }

    private static string GetUserGroup(Guid userId)
    {
        return $"chat-user-{userId}";
    }
}