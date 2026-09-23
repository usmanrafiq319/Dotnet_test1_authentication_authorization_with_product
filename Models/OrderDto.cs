namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class OrderDto
    {
        public Guid UserId { get; set; }

        public DateTime OrderTime { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public List<OrderItemDto> OrderItems { get; set; }  = new();
    }
}
