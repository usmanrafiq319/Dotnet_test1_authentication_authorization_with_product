using Dotnet_test1_authentication_authorization_with_product.Data;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Dotnet_test1_authentication_authorization_with_product.Services
{

    public class OtpService(UserDbContext context,IEmailService emailService,ILogger<OtpService> logger,IConfiguration configuration) : IOtpService
    {
        private readonly UserDbContext _context = context;
        private readonly IEmailService _emailService= emailService;
        private readonly ILogger<OtpService> _logger= logger;
        private readonly IConfiguration _configuration= configuration;



        public async Task<OtpResult> GenerateAndSendOtpAsync(Guid userId, OtpPurpose purpose)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Profile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return OtpResult.Failure("User not found");
                }

                // Invalidate any existing valid OTPs for this purpose
                await InvalidateUserOtpsAsync(userId, purpose);

                // Generate 6-digit OTP
                string otpCode = GenerateSecureOtpCode();
                string otpHash = HashOtpCode(otpCode);

                // Create new OTP entity
                var otp = new Otp
                {
                    UserId = userId,
                    CodeHash = otpHash,
                    Purpose = purpose,
                    ExpireAt = DateTime.UtcNow.AddMinutes(GetOtpExpiryMinutes()),
                    Used = false
                };

                _context.Otps.Add(otp);
                await _context.SaveChangesAsync();

                // Send OTP via email
                string userEmail = user.Profile?.Email ?? string.Empty;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    bool emailSent = await SendOtpEmailAsync(userEmail, otpCode, purpose);
                    if (!emailSent)
                    {
                        return OtpResult.Failure("Failed to send OTP email");
                    }
                }
                else
                {
                    return OtpResult.Failure("User email not found");
                }

                _logger.LogInformation($"OTP generated for user {userId} with purpose {purpose}");

                return OtpResult.Success(otp.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating OTP for user {userId}");
                return OtpResult.Failure("An error occurred while generating OTP");
            }
        }

        public async Task<OtpValidationResult> ValidateOtpAsync(Guid userId, string code, OtpPurpose purpose)
        {
            try
            {
                var otp = await _context.Otps
                    .Where(o => o.UserId == userId
                                && o.Purpose == purpose
                                && !o.Used
                                && o.ExpireAt > DateTime.UtcNow)
                    .OrderByDescending(o => o.ExpireAt)
                    .FirstOrDefaultAsync();

                if (otp == null)
                {
                    return OtpValidationResult.Failure("No valid OTP found. Please request a new one.");
                }

                if (!VerifyOtpCode(code, otp.CodeHash))
                {
                    return OtpValidationResult.Failure("Invalid OTP code. Please try again.");
                }

                otp.Used = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"OTP validated for user {userId} with purpose {purpose}");

                return OtpValidationResult.Success(otp.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating OTP for user {userId}");
                return OtpValidationResult.Failure("An error occurred while validating OTP");
            }
        }

        public async Task<bool> MarkOtpAsUsedAsync(Guid otpId)
        {
            try
            {
                var otp = await _context.Otps.FindAsync(otpId);
                if (otp == null)
                {
                    return false;
                }

                otp.Used = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking OTP {otpId} as used");
                return false;
            }
        }

        public async Task<bool> InvalidateUserOtpsAsync(Guid userId, OtpPurpose purpose)
        {
            try
            {
                var otps = await _context.Otps
                    .Where(o => o.UserId == userId
                                && o.Purpose == purpose
                                && !o.Used)
                    .ToListAsync();

                foreach (var otp in otps)
                {
                    otp.Used = true;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error invalidating OTPs for user {userId}");
                return false;
            }
        }

        public async Task<Otp?> GetValidOtpAsync(Guid userId, OtpPurpose purpose)
        {
            return await _context.Otps
                .Where(o => o.UserId == userId
                            && o.Purpose == purpose
                            && !o.Used
                            && o.ExpireAt > DateTime.UtcNow)
                .OrderByDescending(o => o.ExpireAt)
                .FirstOrDefaultAsync();
        }

        public async Task CleanupExpiredOtpsAsync()
        {
            try
            {
                var expiredOtps = await _context.Otps
                    .Where(o => o.ExpireAt <= DateTime.UtcNow && !o.Used)
                    .Take(1000)
                    .ToListAsync();

                if (expiredOtps.Any())
                {
                    _context.Otps.RemoveRange(expiredOtps);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Cleaned up {expiredOtps.Count} expired OTPs");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired OTPs");
            }
        }

        #region Private Helper Methods

        private string GenerateSecureOtpCode()
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[4];
            rng.GetBytes(bytes);
            int randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0));
            return (randomNumber % 900000 + 100000).ToString();
        }

        private string HashOtpCode(string otpCode)
        {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(otpCode));
            return Convert.ToBase64String(hashBytes);
        }

        private bool VerifyOtpCode(string inputCode, string storedHash)
        {
            string inputHash = HashOtpCode(inputCode);
            return string.Equals(inputHash, storedHash, StringComparison.Ordinal);
        }

        private int GetOtpExpiryMinutes()
        {
            return _configuration.GetValue<int>("OtpSettings:ExpiryMinutes", 10);
        }

        private async Task<bool> SendOtpEmailAsync(string userEmail, string otpCode, OtpPurpose purpose)
        {
            try
            {
                string subject = purpose == OtpPurpose.PasswordReset
                    ? "Password Reset OTP"
                    : "Email Verification OTP";

                string htmlBody = GetOtpEmailHtml(otpCode, purpose);

                await _emailService.SendEmailAsync(userEmail, subject, htmlBody);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send OTP email to {userEmail}");
                return false;
            }
        }

        private string GetOtpEmailHtml(string otpCode, OtpPurpose purpose)
        {
            string purposeText = purpose == OtpPurpose.PasswordReset
                ? "reset your password"
                : "verify your email address";

            int expiryMinutes = GetOtpExpiryMinutes();

            return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 5px; }}
                .otp-code {{ font-size: 32px; font-weight: bold; color: #007bff; padding: 20px; text-align: center; letter-spacing: 5px; }}
                .footer {{ margin-top: 30px; font-size: 14px; color: #6c757d; text-align: center; }}
                .warning {{ color: #dc3545; font-size: 14px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h2>Your OTP Code</h2>
                </div>
                <div style='padding: 20px;'>
                    <p>Hello,</p>
                    <p>You have requested to {purposeText}. Use the following OTP code to complete the process:</p>
                    <div class='otp-code'>{otpCode}</div>
                    <p>This OTP is valid for {expiryMinutes} minutes.</p>
                    <p class='warning'>If you did not request this, please ignore this email.</p>
                </div>
                <div class='footer'>
                    <p>This is an automated message, please do not reply to this email.</p>
                </div>
            </div>
        </body>
        </html>";
        }

        #endregion
    }

    public class OtpResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? OtpId { get; set; }

        public static OtpResult Success(Guid otpId) => new()
        {
            IsSuccess = true,
            OtpId = otpId,
            Message = "OTP generated and sent successfully"
        };

        public static OtpResult Failure(string message) => new()
        {
            IsSuccess = false,
            Message = message
        };
    }

    public class OtpValidationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? OtpId { get; set; }

        public static OtpValidationResult Success(Guid otpId) => new()
        {
            IsSuccess = true,
            OtpId = otpId,
            Message = "OTP validated successfully"
        };

        public static OtpValidationResult Failure(string message) => new()
        {
            IsSuccess = false,
            Message = message
        };
    }

}
