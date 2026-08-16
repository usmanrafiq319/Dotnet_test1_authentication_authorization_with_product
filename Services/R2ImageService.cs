using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Dotnet_test1_authentication_authorization_with_product.Configuration;
using Dotnet_test1_authentication_authorization_with_product.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dotnet_test1_authentication_authorization_with_product.Services
{
    public class R2ImageService : IR2ImageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly R2Storage _options;

        public R2ImageService(IAmazonS3 s3Client, IOptions<R2Storage> options)
        {
            _s3Client = s3Client;
            _options = options.Value;
        }

        /// <summary>
        /// Centralized key extraction logic. Strips out PublicUrl and host details if present, 
        /// returning clean relative object keys like "avatars/guid.jpg" or "products/guid.jpg".
        /// </summary>
        private string ExtractObjectKey(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return string.Empty;

            // Handle legacy/external non-R2 links gracefully
            if (imageUrl.Contains("gstatic.com") || imageUrl.Contains("googleusercontent.com"))
            {
                return imageUrl;
            }

            var key = imageUrl;

            // Remove configured base public URL if present
            if (!string.IsNullOrEmpty(_options.PublicUrl) && key.StartsWith(_options.PublicUrl, StringComparison.OrdinalIgnoreCase))
            {
                key = key.Replace($"{_options.PublicUrl}/", "").Replace(_options.PublicUrl, "");
            }

            // Extract relative absolute path if full absolute URI is provided
            if (Uri.TryCreate(key, UriKind.Absolute, out var uri))
            {
                key = uri.AbsolutePath;
            }

            return key.TrimStart('/');
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder = "products")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null.");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif", "image/jpg" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                throw new ArgumentException($"File type '{file.ContentType}' is not allowed.");

            if (file.Length > 2 * 1024 * 1024) // 2MB limit
                throw new ArgumentException("File size cannot exceed 2MB.");

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var cleanFolder = folder.Trim('/');
            var key = $"{cleanFolder}/{fileName}";

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = key,
                    InputStream = memoryStream,
                    ContentType = file.ContentType,
                    UseChunkEncoding = false // Critical for Cloudflare R2 compatibility
                };

                var response = await _s3Client.PutObjectAsync(putRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    return $"{_options.PublicUrl.TrimEnd('/')}/{key}";
                }

                throw new Exception($"Upload failed with status code: {response.HttpStatusCode}");
            }
            catch (AmazonS3Exception)
            {
                // Fallback attempt using TransferUtility
                try
                {
                    memoryStream.Position = 0;
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = memoryStream,
                        Key = key,
                        BucketName = _options.BucketName,
                        ContentType = file.ContentType
                    };

                    var fileTransferUtility = new TransferUtility(_s3Client);
                    await fileTransferUtility.UploadAsync(uploadRequest);

                    return $"{_options.PublicUrl.TrimEnd('/')}/{key}";
                }
                catch (Exception innerEx)
                {
                    throw new Exception($"Upload failed: {innerEx.Message}");
                }
            }
        }

        public async Task<R2ImageResponseDto> GetImageAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("Image URL or key cannot be empty.");

            var key = ExtractObjectKey(imageUrl);

            var request = new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(request);

            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return new R2ImageResponseDto
            {
                Stream = memoryStream,
                ContentType = response.Headers.ContentType ?? "image/jpeg"
            };
        }

        public async Task<GetObjectResponse> GetImageResponseAsync(string imageUrl)
        {
            try
            {
                var key = ExtractObjectKey(imageUrl);

                var request = new GetObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = key
                };

                return await _s3Client.GetObjectAsync(request);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new FileNotFoundException($"Image not found: {imageUrl}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve image response: {ex.Message}");
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                var key = ExtractObjectKey(imageUrl);
                if (string.IsNullOrEmpty(key)) return false;

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = key
                };

                var response = await _s3Client.DeleteObjectAsync(deleteRequest);
                var statusCode = (int)response.HttpStatusCode;
                return statusCode >= 200 && statusCode < 300;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ImageExistsAsync(string imageUrl)
        {
            try
            {
                var key = ExtractObjectKey(imageUrl);
                if (string.IsNullOrEmpty(key)) return false;

                var request = new GetObjectMetadataRequest
                {
                    BucketName = _options.BucketName,
                    Key = key
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> UploadMultipleImagesAsync(List<IFormFile> files, string folder = "products")
        {
            var urls = new List<string>();
            foreach (var file in files)
            {
                var url = await UploadImageAsync(file, folder);
                urls.Add(url);
            }
            return urls;
        }

        public async Task<List<string>> ListImagesAsync(string folder = "products")
        {
            try
            {
                var cleanFolder = folder.Trim('/');
                var request = new ListObjectsRequest
                {
                    BucketName = _options.BucketName,
                    Prefix = $"{cleanFolder}/"
                };

                var response = await _s3Client.ListObjectsAsync(request);

                var imageUrls = response.S3Objects
                    .Where(obj => !obj.Key.EndsWith("/"))
                    .Select(obj => $"{_options.PublicUrl.TrimEnd('/')}/{obj.Key}")
                    .ToList();

                return imageUrls;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to list images from folder '{folder}': {ex.Message}", ex);
            }
        }
    }
}