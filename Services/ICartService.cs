using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Models;
using Microsoft.EntityFrameworkCore;

namespace Dotnet_test1_authentication_authorization_with_product.Services
{
    public interface ICartService
    {
        Task<CartDto?> AddCartItemAsync(Guid userId, AddCartItemDto cartItem);
        Task<CartDto?> GetCartAsync(Guid userId);
        Task<int?> GetCartItemQuanityAsync(Guid userId, Guid productId);
    }
}
