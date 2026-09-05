using System.ComponentModel.DataAnnotations;

namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class CreateProductDto
    {
        public Guid Id { get; set; } // Added ID field successfully

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public int Price { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // REMOVE [Required] here so updates don't break when keeping the old image
        public IFormFile? Image { get; set; }
    }
}
