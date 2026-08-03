namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class ProfileDto
    {
        public Guid? Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
