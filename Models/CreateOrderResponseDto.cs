namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class CreateOrderResponseDto
    {
        public Guid OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
