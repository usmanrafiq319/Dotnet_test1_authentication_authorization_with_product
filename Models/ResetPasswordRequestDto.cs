namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class ResetPasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
