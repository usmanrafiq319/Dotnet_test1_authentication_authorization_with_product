using Dotnet_test1_authentication_authorization_with_product.Data;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Model;
using Dotnet_test1_authentication_authorization_with_product.Models;
using Dotnet_test1_authentication_authorization_with_product.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Dotnet_test1_authentication_authorization_with_product.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class authController( IAuthService authService, IEmailService emailService, IOtpService otpService, ILogger<authController> logger, IMemoryCache cache) : ControllerBase

    {
        private readonly IAuthService _authService = authService;
        private readonly IEmailService _emailService = emailService;
        private readonly IOtpService _otpService = otpService;
        private readonly ILogger<authController> _logger = logger;
        private readonly IMemoryCache _cache = cache;

        // ... Your existing endpoints (Register, Login, TokenRequest, Logout) ...


        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { message = "Email is required." });
                }

                var user = await _authService.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    // Don't reveal if email exists for security
                    return Ok(new { message = "If the email exists, you will receive an OTP." });
                }

                var result = await _otpService.GenerateAndSendOtpAsync(user.Id, OtpPurpose.PasswordReset);

                if (!result.IsSuccess)
                {
                    return StatusCode(500, new { message = "Failed to generate OTP. Please try again." });
                }

                return Ok(new { message = "OTP sent to your email address." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ForgotPassword endpoint");
                return StatusCode(500, new { message = "An error occurred." });
            }
        }


        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Code))
                {
                    return BadRequest(new { message = "Email and OTP code are required." });
                }

                var user = await _authService.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new { message = "User not found." });
                }

                var validationResult = await _otpService.ValidateOtpAsync(
                    user.Id,
                    request.Code,
                    OtpPurpose.PasswordReset);

                if (!validationResult.IsSuccess)
                {
                    return BadRequest(new { message = validationResult.Message });
                }

                // Generate a temporary session token for password reset
                var resetSessionToken = GenerateResetSessionToken(user.Id);

                return Ok(new
                {
                    message = "OTP verified successfully.",
                    resetToken = resetSessionToken,
                    email = request.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VerifyOtp endpoint");
                return StatusCode(500, new { message = "An error occurred." });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) ||
                    string.IsNullOrEmpty(request.ResetToken) ||
                    string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new { message = "All fields are required." });
                }

                // Validate the reset session token
                var userId = ValidateResetSessionToken(request.ResetToken);
                if (userId == null)
                {
                    return BadRequest(new { message = "Invalid or expired reset session. Please request a new OTP." });
                }

                // Verify the user matches the email
                var user = await _authService.GetUserByEmailAsync(request.Email);
                if (user == null || user.Id != userId)
                {
                    return BadRequest(new { message = "User mismatch." });
                }

                // Reset password
                var result = await _authService.ResetPasswordAsync(user.Id, request.NewPassword);
                if (!result)
                {
                    return BadRequest(new { message = "Failed to reset password." });
                }

                // Clear the user's session
                var sessionCleared = await ClearUserSessionAsync();

                if (!sessionCleared)
                {
                    // Session wasn't cleared in database, but cookie is cleared
                    // You can still return success with a warning
                    return Ok(new
                    {
                        message = "Password reset successfully. Your session has been cleared, but there may have been issues with token invalidation. Please login with your new password.",
                        sessionCleared = false,
                        warning = "Previous session may still be active on other devices"
                    });
                }

                return Ok(new
                {
                    message = "Password reset successfully and previous sessions cleared. Please login with your new password.",
                    sessionCleared = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetPassword endpoint");
                return StatusCode(500, new { message = "An error occurred." });
            }
        }
        
        [NonAction]
        private string GenerateResetSessionToken(Guid userId)
        {
            // Create a secure random token
            using var rng = RandomNumberGenerator.Create();
            byte[] tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            string token = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            // Store in memory cache with expiration (3 minutes)
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.UtcNow.AddMinutes(3)
            };

            _cache.Set(token, userId, cacheOptions);

            return token;
        }

        [NonAction]
        private Guid? ValidateResetSessionToken(string token)
        {
            if (_cache.TryGetValue(token, out Guid userId))
            {
                _cache.Remove(token); // One-time use
                return userId;
            }
            return null;
        }


        // ... Your existing endpoints (Register, Login, AccessToken, Logout, TestEmail) ...

        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmail()
        {
            await _emailService.SendEmailAsync(
                "usmanrafiqghani@gmail.com",
                "Test Email",
                "<h2>Email service is working!</h2>");

            return Ok();
        }


        [HttpPost("register")]
        public async Task<ActionResult<AccessTokenDto?>> Register(RegisterDto request)
        {
            var token = await _authService.RegisterAsync(request);
            if(token is null)
            {
                return BadRequest("User name already exists");
            }

            SetRefreshTokenCookie(token.RefreshToken, token.RefreshTokenExpiaryTime);

            AccessTokenDto accesstoken = new()
            {AccessToken = token.AccessToken};

            return Ok(accesstoken);
        }


        [HttpPost("login")]
        public async Task<ActionResult<AccessTokenDto?>> Login(UserDto request)
        {
            var token = await _authService.LoginUserAsync(request);
            if (token is null)
            {
                return Unauthorized("Refresh token expired or user don't exists or token mismatch"); 
            }

            SetRefreshTokenCookie(token.RefreshToken,token.RefreshTokenExpiaryTime);
            AccessTokenDto accesstoken = new() { 
            AccessToken=token.AccessToken
            };

            return Ok(accesstoken);
        }


        [Authorize]
        [HttpGet("user-id")]
        public IActionResult GetId()
        {
            Guid UserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(UserId);

        }

        [Authorize]
        [HttpGet("role")] 
        public IActionResult GetRole()
        {
            // Extracts the role claim. Tries both standard WS-Federation and short JWT formats.
            string userRole = User.FindFirst(ClaimTypes.Role)?.Value
                              ?? User.FindFirst("role")!.Value;

            return Ok(userRole);
        }


        [HttpPost("access-token")]
        public async Task<ActionResult<AccessTokenDto>> TokenRequest()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out string refreshToken))
            {
                return Unauthorized("No refresh token found.");
            }
            var Token = await _authService.TokenRequestAsync(refreshToken);
            if(Token is null)
            {
                return BadRequest($"need to login again");
            }
                // 4. (Optional) Rotate the refresh token for maximum security
             SetRefreshTokenCookie(Token.RefreshToken,Token.RefreshTokenExpiaryTime);
            return Ok(Token.AccessToken);
        }

        [NonAction]
        private void SetRefreshTokenCookie(string refreshToken,DateTime refreshTokenExpiryTime)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = new DateTimeOffset(
                    refreshTokenExpiryTime,
                    TimeSpan.Zero
                ),
                Path = "/"
            };

            Response.Cookies.Append(
                "refreshToken",
                refreshToken,
                cookieOptions
            );
        }
       
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            bool validator = await ClearUserSessionAsync();

            if (validator is true)
            {
                return Ok(new { message = "Logged out successfully" });
            }

            return BadRequest(new { error = "already logged out or an issue occurred" });
        }

        [NonAction]
        private async Task<bool> ClearUserSessionAsync()
        {
            bool sessionCleared = false;

            try
            {
                // 1. Check for refresh token in cookies
                if (Request.Cookies.TryGetValue("refreshToken", out string? refreshToken) &&
                    !string.IsNullOrEmpty(refreshToken))
                {
                    // 2. Invalidate token in database (Supabase via Auth Service)
                    var logoutResult = await _authService.LogoutAsync(refreshToken);

                    if (logoutResult is false)
                    {
                        _logger.LogInformation("Refresh token was already invalid or expired");
                        sessionCleared = false;
                    }
                    else
                    {
                        sessionCleared = true;
                    }
                }
                else
                {
                    _logger.LogInformation("No refresh token found in cookies");
                    sessionCleared = false;
                }

                // 3. ALWAYS destroy the browser cookie for cross-origin setups
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddDays(-1), // Forces immediate deletion
                    Path = "/"                                   // Matches cookie initialization scope
                };
                Response.Cookies.Append("refreshToken", "", cookieOptions);

                return sessionCleared;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred while clearing user session");

                // 4. Fallback execution block to guarantee cookie is dropped on client side
                try
                {
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        Expires = DateTimeOffset.UtcNow.AddDays(-1),
                        Path = "/"
                    };
                    Response.Cookies.Append("refreshToken", "", cookieOptions);
                }
                catch { }

                return false;
            }
        }

        [Authorize(Roles ="Admin")]
        [HttpGet("admin-only")]
        public ActionResult<string> checkAdmin()
        {
            return Ok("you are authorized admin");
        }


        [Authorize]
        [HttpGet]
        public ActionResult<string> checkAuthorization()
        {
            return Ok("you are authorized");
        }
    
    
    }
}








  






