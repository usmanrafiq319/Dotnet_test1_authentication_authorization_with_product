using Dotnet_test1_authentication_authorization_with_product.Data;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Models;
using Dotnet_test1_authentication_authorization_with_product.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Dotnet_test1_authentication_authorization_with_product.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class chatController : ControllerBase
{
    private readonly IChatService _chatService;

    public chatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("my-conversation")]
    public async Task<IActionResult>
    GetMyConversation(CancellationToken cancellationToken)
    {
        if (
            User.IsInRole(
                UserRole.Admin.ToString()
            )
        )
        {
            return BadRequest(
                "Admins must use admin endpoints."
            );
        }

        var conversation =
            await _chatService
                .GetMyConversationAsync(
                    GetCurrentUserId(),
                    cancellationToken
                );

        return Ok(conversation);
    }

    [HttpGet("admin/summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
    GetAdminSummary(CancellationToken cancellationToken)
    {
        var summary =
            await _chatService
                .GetAdminSummaryAsync(
                    cancellationToken
                );

        return Ok(summary);
    }

    [HttpGet("admin/conversations")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
    GetAllConversations(CancellationToken cancellationToken)
    {
        var conversations =
            await _chatService
                .GetAllConversationsAsync(
                    cancellationToken
                );

        return Ok(conversations);
    }

    [HttpGet(
        "admin/conversations/{conversationId:guid}"
    )]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
    GetConversation(Guid conversationId,CancellationToken cancellationToken)
    {
        var conversation =
            await _chatService
                .GetConversationAsync(
                    conversationId,
                    cancellationToken
                );

        if (conversation is null)
        {
            return NotFound(
                "Conversation not found."
            );
        }

        return Ok(conversation);
    }

    private Guid GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        if (
            !Guid.TryParse(
                value,
                out var userId
            )
        )
        {
            throw new UnauthorizedAccessException(
                "Authenticated user ID is invalid."
            );
        }

        return userId;
    }
}