using Microsoft.VisualBasic;

namespace Dotnet_test1_authentication_authorization_with_product.Entities
{
    public class Cart
    {    
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public DateTime OrderTime { get; set; } = DateTime.UtcNow;

    }
}
