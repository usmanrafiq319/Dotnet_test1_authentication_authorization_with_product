namespace Dotnet_test1_authentication_authorization_with_product.Entities
{
    public class OrderItem
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }
        public Order? Order { get; set; }

        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        public int Quantity { get; set; }

        // Price at the time the order was placed
        public int UnitPrice { get; set; }

        public int SubTotal => UnitPrice * Quantity;
    }
}
