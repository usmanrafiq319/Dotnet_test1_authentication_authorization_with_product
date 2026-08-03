namespace Dotnet_test1_authentication_authorization_with_product.Models
{
    public class CreateProfileDto
    {
        public  string? Email { get; set; }

        // This accepts the uploaded file from the client
        public  IFormFile? Image { get; set; }
    }
}
