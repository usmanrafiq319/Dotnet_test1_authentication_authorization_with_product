namespace Dotnet_test1_authentication_authorization_with_product.Entities
{
    public class Order
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public DateTime OrderTime { get; set; } = DateTime.UtcNow;
    }
    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }
}
