using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dotnet_test1_authentication_authorization_with_product.Services;

namespace Dotnet_test1_authentication_authorization_with_product.Configuration
{


  
        [ApiController]
        [Route("api/[controller]")]
        [Produces("application/json")]
        public class ProductImageController : ControllerBase
        {
            private readonly R2ImageService _imageService;
            private readonly ILogger<ProductImageController> _logger;

            public ProductImageController(
                R2ImageService imageService,
                ILogger<ProductImageController> logger)
            {
                _imageService = imageService;
                _logger = logger;
            }

            /// <summary>
            /// Test the R2 storage connection
            /// </summary>
            [HttpGet("test")]
            [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
            public IActionResult TestConnection()
            {
                return Ok(new
                {
                    success = true,
                    status = "R2 Storage is configured successfully!",
                    bucket = "my-ecom-app-images",
                    publicUrl = "https://pub-4c7e1183e124c1d45bbf13c34bc69af3.r2.dev/my-ecom-app-images",
                    endpoints = new
                    {
                        upload = "POST /api/ProductImage/upload",
                        uploadMultiple = "POST /api/ProductImage/upload-multiple",
                        delete = "DELETE /api/ProductImage/delete?imageUrl={url}",
                        exists = "GET /api/ProductImage/exists?imageUrl={url}",
                        list = "GET /api/ProductImage/list?folder={folder}",
                        metadata = "GET /api/ProductImage/metadata?imageUrl={url}"
                    },
                    timestamp = DateTime.UtcNow
                });
            }

            /// <summary>
            /// Upload a single image
            /// </summary>
            [HttpPost("upload")]
            [RequestSizeLimit(3 * 1024 * 1024)] // 10MB limit
            [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
            {
                try
                {
                    // Validate file
                    if (file == null || file.Length == 0)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "No file uploaded",
                            code = "NO_FILE"
                        });
                    }

                    // Validate file type
                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif", "image/jpg" };
                    if (!allowedTypes.Contains(file.ContentType.ToLower()))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"File type '{file.ContentType}' is not allowed. Allowed types: {string.Join(", ", allowedTypes)}",
                            code = "INVALID_FILE_TYPE"
                        });
                    }

                    // Validate file size
                    if (file.Length > 3 * 1024 * 1024)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "File size cannot exceed 10MB",
                            code = "FILE_TOO_LARGE"
                        });
                    }

                    _logger.LogInformation($"Uploading file: {file.FileName}, Size: {file.Length} bytes");

                    var imageUrl = await _imageService.UploadImageAsync(file, "products");

                    _logger.LogInformation($"File uploaded successfully: {imageUrl}");

                    return Ok(new
                    {
                        success = true,
                        imageUrl = imageUrl,
                        fileName = file.FileName,
                        fileSize = $"{file.Length / 1024} KB",
                        fileSizeBytes = file.Length,
                        contentType = file.ContentType,
                        message = "Image uploaded successfully",
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning($"Upload validation error: {ex.Message}");
                    return BadRequest(new
                    {
                        success = false,
                        message = ex.Message,
                        code = "VALIDATION_ERROR"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "An error occurred while uploading the image",
                        error = ex.Message,
                        code = "UPLOAD_ERROR"
                    });
                }
            }

            /// <summary>
            /// Upload multiple images
            /// </summary>
            [HttpPost("upload-multiple")]
            [RequestSizeLimit(20 * 1024 * 1024)] // 20MB total limit
            [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> UploadMultipleImages([FromForm] List<IFormFile> files)
            {
                try
                {
                    // Validate files
                    if (files == null || files.Count == 0)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "No files uploaded",
                            code = "NO_FILES"
                        });
                    }

                    if (files.Count > 10)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Maximum 10 files allowed per upload",
                            code = "TOO_MANY_FILES"
                        });
                    }

                    // Validate each file
                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif", "image/jpg" };
                    var invalidFiles = new List<string>();

                    foreach (var file in files)
                    {
                        if (!allowedTypes.Contains(file.ContentType.ToLower()))
                        {
                            invalidFiles.Add($"{file.FileName} ({file.ContentType})");
                        }
                        if (file.Length > 3 * 1024 * 1024)
                        {
                            invalidFiles.Add($"{file.FileName} (exceeds 10MB)");
                        }
                    }

                    if (invalidFiles.Any())
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Some files are invalid",
                            invalidFiles = invalidFiles,
                            code = "INVALID_FILES"
                        });
                    }

                    _logger.LogInformation($"Uploading {files.Count} files");

                    var imageUrls = await _imageService.UploadMultipleImagesAsync(files, "products");

                    _logger.LogInformation($"Successfully uploaded {imageUrls.Count} files");

                    return Ok(new
                    {
                        success = true,
                        imageUrls = imageUrls,
                        count = imageUrls.Count,
                        message = $"{imageUrls.Count} images uploaded successfully",
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading multiple files");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "An error occurred while uploading images",
                        error = ex.Message,
                        code = "UPLOAD_ERROR"
                    });
                }
            }

            /// <summary>
            /// Delete an image by URL
            /// </summary>
            [HttpDelete("delete")]
            [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> DeleteImage([FromQuery] string imageUrl)
            {
                try
                {
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Image URL is required",
                            code = "MISSING_URL"
                        });
                    }

                    // Validate URL format
                    if (!imageUrl.StartsWith("https://pub-4c7e1183e124c1d45bbf13c34bc69af3.r2.dev/my-ecom-app-images/"))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Invalid image URL format",
                            code = "INVALID_URL"
                        });
                    }

                    _logger.LogInformation($"Deleting image: {imageUrl}");

                    var result = await _imageService.DeleteImageAsync(imageUrl);

                    if (result)
                    {
                        _logger.LogInformation($"Image deleted successfully: {imageUrl}");
                        return Ok(new
                        {
                            success = true,
                            message = "Image deleted successfully",
                            imageUrl = imageUrl,
                            timestamp = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        return NotFound(new
                        {
                            success = false,
                            message = "Image not found or could not be deleted",
                            imageUrl = imageUrl,
                            code = "NOT_FOUND"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting image");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "An error occurred while deleting the image",
                        error = ex.Message,
                        code = "DELETE_ERROR"
                    });
                }
            }

            /// <summary>
            /// Check if an image exists
            /// </summary>
            [HttpGet("exists")]
            [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> CheckImageExists([FromQuery] string imageUrl)
            {
                try
                {
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Image URL is required",
                            code = "MISSING_URL"
                        });
                    }

                    _logger.LogInformation($"Checking if image exists: {imageUrl}");

                    var exists = await _imageService.ImageExistsAsync(imageUrl);

                    return Ok(new
                    {
                        success = true,
                        exists = exists,
                        imageUrl = imageUrl,
                        message = exists ? "Image exists" : "Image does not exist",
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking image existence");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "An error occurred while checking the image",
                        error = ex.Message,
                        code = "CHECK_ERROR"
                    });
                }
            }

            /// <summary>
            /// List all images in a folder
            /// </summary>
            [HttpGet("list")]
            [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> ListImages([FromQuery] string folder = "products")
            {
                try
                {
                    if (string.IsNullOrEmpty(folder))
                    {
                        folder = "products";
                    }

                    _logger.LogInformation($"Listing images in folder: {folder}");

                    var images = await _imageService.ListImagesAsync(folder);

                    return Ok(new
                    {
                        success = true,
                        folder = folder,
                        count = images.Count,
                        images = images,
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error listing images");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "An error occurred while listing images",
                        error = ex.Message,
                        code = "LIST_ERROR"
                    });
                }
            }

            /// <summary>
            /// Get image metadata
            /// </summary>
            [HttpGet("metadata")]
            [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> GetImageMetadata([FromQuery] string imageUrl)
            {
                try
                {
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Image URL is required",
                            code = "MISSING_URL"
                        });
                    }

                    _logger.LogInformation($"Getting metadata for: {imageUrl}");

                    var metadata = await _imageService.GetImageMetadataAsync(imageUrl);

                    return Ok(new
                    {
                        success = true,
                        imageUrl = imageUrl,
                        metadata = metadata,
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting image metadata");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "An error occurred while getting image metadata",
                        error = ex.Message,
                        code = "METADATA_ERROR"
                    });
                }
            }

            /// <summary>
            /// Update an image (replace existing)
            /// </summary>
            [HttpPut("update")]
            [RequestSizeLimit(10 * 1024 * 1024)]
            [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> UpdateImage([FromQuery] string imageUrl,[FromForm] IFormFile file)
            {
                try
                {
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Image URL is required",
                            code = "MISSING_URL"
                        });
                    }

                    if (file == null || file.Length == 0)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "No file uploaded",
                            code = "NO_FILE"
                        });
                    }

                    _logger.LogInformation($"Updating image: {imageUrl}");

                    // First, delete the old image
                    var deleteResult = await _imageService.DeleteImageAsync(imageUrl);
                    if (!deleteResult)
                    {
                        return NotFound(new
                        {
                            success = false,
                            message = "Original image not found",
                            code = "ORIGINAL_NOT_FOUND"
                        });
                    }

                    // Then upload the new one
                    var newImageUrl = await _imageService.UploadImageAsync(file, "products");

                    _logger.LogInformation($"Image updated successfully: {newImageUrl}");

                    return Ok(new
                    {
                        success = true,
                        oldImageUrl = imageUrl,
                        newImageUrl = newImageUrl,
                        fileName = file.FileName,
                        fileSize = $"{file.Length / 1024} KB",
                        message = "Image updated successfully",
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating image");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "An error occurred while updating the image",
                        error = ex.Message,
                        code = "UPDATE_ERROR"
                    });
                }
            }
        
    
    }
   
}

