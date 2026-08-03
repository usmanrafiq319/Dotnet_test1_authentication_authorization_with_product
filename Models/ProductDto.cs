namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Price { get; set; }
        public int Quantity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
