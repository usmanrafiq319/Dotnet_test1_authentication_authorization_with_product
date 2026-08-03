namespace Dotnet_test1_authentication_authorization_with_product.Entities
{
    public class Otp
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string CodeHash { get; set; } = string.Empty;

        public OtpPurpose Purpose { get; set; }

        public DateTime ExpireAt { get; set; }

        public bool Used { get; set; }

        public User User { get; set; } = null!;

    }
}
