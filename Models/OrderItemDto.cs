namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class OrderItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int UnitPrice { get; set; }
        public int SubTotal { get; set; }
    }
}
