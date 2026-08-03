using Dotnet_test1_authentication_authorization_with_product.Models;
using Dotnet_test1_authentication_authorization_with_product.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dotnet_test1_authentication_authorization_with_product.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class cartController(ICartService service) : ControllerBase

    {
        private ICartService _service = service;
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CartDto>> AddCartItem(AddCartItemDto item)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var listItems = await _service.AddCartItemAsync(userId, item);
            return Ok(listItems);

        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<CartDto>> GetCart()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }
            var list = await _service.GetCartAsync(userId);
            if(list is null)
            {
                return NoContent();
            }
            return Ok(list);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<int>> GetCartItemQuanity(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }
            var cartitem = await _service.GetCartItemQuanityAsync(userId, id);
            return Ok(cartitem);

        }
    }
}
