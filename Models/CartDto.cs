namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class CartDto
    {
        public List<CartItemDto> CartItems { get; set; } = new();
        public int Total { get; set; }
    }
}
