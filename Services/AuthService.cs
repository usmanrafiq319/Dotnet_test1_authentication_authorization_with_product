using Dotnet_test1_authentication_authorization_with_product.Data;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Model;
using Dotnet_test1_authentication_authorization_with_product.Models;
using Dotnet_test1_authentication_authorization_with_product.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Dotnet_test1_authentication_authorization_with_product.Services
{
    public class AuthService(IConfiguration configuration, UserDbContext context,JwtSecurityTokenHandler tokenHandler,IPasswordHasher<User> passwordHasher,ILogger<AuthService> _logger) : IAuthService
    {
        private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
        private readonly JwtSecurityTokenHandler _tokenHandler = tokenHandler;
        private readonly IConfiguration _configuration = configuration;
        private readonly UserDbContext _context = context;

        // NEW: Password Reset Methods
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Profile)
                    .Include(u => u.Otps)
                    .FirstOrDefaultAsync(u => u.Profile != null && u.Profile.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return null;
            }
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Profile)
                    .Include(u => u.Otps)
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for password reset: {UserId}", userId);
                    return false;
                }

                // Hash the new password
                user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

                // Invalidate all OTPs for this user after successful password reset
                var userOtps = await _context.Otps
                    .Where(o => o.UserId == userId && !o.Used)
                    .ToListAsync();

                foreach (var otp in userOtps)
                {
                    otp.Used = true;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successfully for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateUserPasswordAsync(Guid userId, string newPassword)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password updated for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for user: {UserId}", userId);
                return false;
            }
        }
  
        // ... Your existing methods (RegisterAsync, LoginUserAsync, TokenRequestAsync, LogoutAsync) ...

        public async Task<TokenDto?> RegisterAsync(RegisterDto request)
        {
            // Username must be unique
            if (await _context.Users.AnyAsync(u => u.UserName == request.UserName))
            {
                return null;
            }

            // Email must also be unique
            if (await _context.Profiles.AnyAsync(p => p.Email == request.Email))
            {
                return null;
            }

            var user = new User();

            user.UserName = request.UserName;

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            var profile = new Profile
            {
                Email = request.Email,
                User = user
            };

            _context.Users.Add(user);
            _context.Profiles.Add(profile);
            user.RefreshTokenExpiaryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return await CreateFullToken(user);
        }

        public async Task<TokenDto?> LoginUserAsync(UserDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == request.UserName);

            if (user is null)
            {
                return null;
            }

            // First verify credentials
            if (_passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password
            ) == PasswordVerificationResult.Failed)
            {
                return null;
            }

            // User already has an active 7-day session
            if (user.RefreshTokenExpiaryTime is not null &&
                user.RefreshTokenExpiaryTime > DateTime.UtcNow)
            {
                return null;
            }

            // Previous session expired.
            // Start a new 7-day session.
            user.RefreshTokenExpiaryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return await CreateFullToken(user);
        }
  
        public async Task<TokenDto?> TokenRequestAsync(string request)
        {
            var user = await ValidatRefreshTokenAsync(request);

            if (user is null)
            {
                return null;
            }

            return await CreateFullToken(user);
        }

        public async Task<bool> LogoutAsync(string request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(user => user.RefreshToken == request);
            if(user is null)
            {
                return false;
            }
            user.RefreshTokenExpiaryTime = null;
            user.RefreshToken = null;
            await _context.SaveChangesAsync();
            return true;

        }
        
        private async Task<TokenDto> CreateFullToken(User? user)
        {
            var CompleteToken = new TokenDto
            {
                RefreshToken = await GenrateAndSaveRefreshTokenAsync(user),
                AccessToken = CreateAccessToken(user),
                RefreshTokenExpiaryTime = user.RefreshTokenExpiaryTime!.Value
            };
            return CompleteToken;
        }

        private async Task<User?> ValidatRefreshTokenAsync( string refreshToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(check=> check.RefreshToken == refreshToken);
            if(user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiaryTime <= DateTime.UtcNow)
            {
                return null;
            }

            return user;

        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new Byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private async Task<string> GenrateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        private string CreateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("AppSettings:Token")!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            // FIX 1: Change to SecurityTokenDescriptor blueprint
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _configuration.GetValue<string>("AppSettings:Issuer"),
                Audience = _configuration.GetValue<string>("AppSettings:Audience"),
                Expires = DateTime.UtcNow.AddMinutes(20),
                SigningCredentials = creds
            };

            // FIX 3: Pass the descriptor blueprint to CreateToken
            var token = _tokenHandler.CreateToken(tokenDescriptor);

            return _tokenHandler.WriteToken(token);
        }

    }
}








 


