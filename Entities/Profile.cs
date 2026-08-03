namespace Dotnet_test1_authentication_authorization_with_product.Entities
{
    public class Profile
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public bool EmailVerified { get; set; } = false;

        public string? ImageUrl { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;
    }
}
