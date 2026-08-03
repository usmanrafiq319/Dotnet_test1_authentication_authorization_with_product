namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class CartItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int Price { get; set; }

    }
}
