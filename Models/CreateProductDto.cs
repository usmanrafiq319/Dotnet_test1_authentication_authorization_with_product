namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class CreateProductDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Price { get; set; }
        public IFormFile? Image { get; set; } // Form file for R2 upload
    }
}
