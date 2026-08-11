using Amazon.S3.Model;
using Dotnet_test1_authentication_authorization_with_product.Models;

namespace Dotnet_test1_authentication_authorization_with_product.Services
{
    public interface IR2ImageService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder = "products");
        Task<List<string>> UploadMultipleImagesAsync(List<IFormFile> files, string folder = "products");
        Task<bool> DeleteImageAsync(string imageUrl);
        Task<bool> ImageExistsAsync(string imageUrl);
        Task<List<string>> ListImagesAsync(string folder = "products");
        Task<ImageMetadataResultDto> GetImageMetadataAsync(string imageUrl);
        Task<Stream> GetImageStreamAsync(string imageUrl);
        Task<GetObjectResponse> GetImageResponseAsync(string imageUrl);
    }
}
