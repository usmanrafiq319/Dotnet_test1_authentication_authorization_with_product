namespace Dotnet_test1_authentication_authorization_with_product.Entities
{
    public class CartItem
    {
        public Guid Id { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }
        public Guid CartId { get; set; }
        public Cart? Cart { get; set; }
        public DateTimeOffset OrderTime { get; set; } = DateTimeOffset.UtcNow;
        public Guid ProductId { get; set; }
        public Product? Product { get; set; }
        //public int SubTotal => Price * Quantity;

    }
}
