using Dotnet_test1_authentication_authorization_with_product.Entities;

namespace Dotnet_test1_authentication_authorization_with_product.Services
{
    public interface IOtpService
    {


        Task<OtpResult> GenerateAndSendOtpAsync(Guid userId, OtpPurpose purpose);
        Task<OtpValidationResult> ValidateOtpAsync(Guid userId, string code, OtpPurpose purpose);
        Task<bool> MarkOtpAsUsedAsync(Guid otpId);
        Task<bool> InvalidateUserOtpsAsync(Guid userId, OtpPurpose purpose);
        Task<Otp?> GetValidOtpAsync(Guid userId, OtpPurpose purpose);
        Task CleanupExpiredOtpsAsync();

    }
}
