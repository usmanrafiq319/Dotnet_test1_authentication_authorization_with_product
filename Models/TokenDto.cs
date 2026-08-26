namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class TokenDto
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiaryTime { get; set; }

    }
}
