using Microsoft.VisualBasic;

namespace Dotnet_test1_authentication_authorization_with_product.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User;
        public string? RefreshToken { get; set; } 
        public DateTime? RefreshTokenExpiaryTime { get; set; }
        public Cart? Cart { get; set; }
        public Profile? Profile { get; set; }

        // Navigation properties
        public ICollection<Otp> Otps { get; set; } = new List<Otp>();

        // One support conversation for a normal user.
        public Conversation? Conversation { get; set; }

    }
}
