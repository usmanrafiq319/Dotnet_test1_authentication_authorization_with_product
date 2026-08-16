using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class CreateProductDto
    {
        [FromForm(Name = "title")]
        [Required]
        public string Title { get; set; } = string.Empty;

        [FromForm(Name = "price")]
        [Required]
        public int Price { get; set; }

        [FromForm(Name = "quantity")]
        [Required]
        public int Quantity { get; set; }

        [FromForm(Name = "description")]
        public string Description { get; set; } = string.Empty;

        [FromForm(Name = "image")]
        public IFormFile? Image { get; set; }
    }
}
