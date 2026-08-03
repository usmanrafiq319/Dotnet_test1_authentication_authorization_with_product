using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Model;
using Dotnet_test1_authentication_authorization_with_product.Models;

namespace Dotnet_test1_authentication_authorization_with_product.Services
{
    public interface IAuthService
    {
        Task<TokenDto?> RegisterAsync(RegisterDto request);
        Task<TokenDto?> LoginUserAsync(UserDto request);
        Task<TokenDto?> TokenRequestAsync(string request);
        Task<bool> LogoutAsync(string request);

        // NEW: Password Reset Methods (using OTP)
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<bool> ResetPasswordAsync(Guid userId, string newPassword);
        Task<bool> UpdateUserPasswordAsync(Guid userId, string newPassword);

    }
}
