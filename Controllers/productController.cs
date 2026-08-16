using Dotnet_test1_authentication_authorization_with_product.Data;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Models;
using Dotnet_test1_authentication_authorization_with_product.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dotnet_test1_authentication_authorization_with_product.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class productController(UserDbContext context, IR2ImageService r2ImageService) : ControllerBase
    {
        private readonly UserDbContext _context = context;
        private readonly IR2ImageService _r2ImageService = r2ImageService;

        //[Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreatProduct( CreateProductDto request)
        {
            if (await _context.Products.AnyAsync(item => item.Title == request.Title))
            {
                return BadRequest("Product title already exists.");
            }

            string imageUrl = string.Empty;

            if (request.Image != null && request.Image.Length > 0)
            {
                try
                {
                    // Uploads binary file directly to R2 under 'products/' prefix
                    imageUrl = await _r2ImageService.UploadImageAsync(request.Image, "products");
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Image upload failed: {ex.Message}");
                }
            }

            var saveProduct = new Product
            {
                Title = request.Title,
                Description = request.Description,
                Quantity = request.Quantity,
                Price = request.Price,
                Url = imageUrl
            };

            _context.Products.Add(saveProduct);
            await _context.SaveChangesAsync();

            return Ok(new ProductDto
            {
                Id = saveProduct.Id,
                Title = saveProduct.Title,
                Description = saveProduct.Description,
                Quantity = saveProduct.Quantity,
                Price = saveProduct.Price,
                Url = GetProductImageUrl(saveProduct.Id, saveProduct.Url)
            });
        }

        [HttpGet]
        public async Task<ActionResult<List<ProductDto>>> GetAllProduct()
        {
            var list = await _context.Products.ToListAsync();

            if (list is null || list.Count == 0)
            {
                return NotFound("No products found.");
            }

            var productDtos = list.Select(p => new ProductDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Quantity = p.Quantity,
                Price = p.Price,
                Url = GetProductImageUrl(p.Id, p.Url)
            }).ToList();

            return Ok(productDtos);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProductDto>> GetSingleProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product is null)
            {
                return NotFound("The product with this ID does not exist.");
            }

            return Ok(new ProductDto
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description,
                Quantity = product.Quantity,
                Price = product.Price,
                Url = GetProductImageUrl(product.Id, product.Url)
            });
        }

        // GET: api/product/{id}/image - Streams binary image content directly from Cloudflare R2
        [HttpGet("{id:guid}/image")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetProductImage(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null || string.IsNullOrEmpty(product.Url))
            {
                return NotFound("Product or image reference not found.");
            }

            try
            {
                var r2Response = await _r2ImageService.GetImageAsync(product.Url);
                if (r2Response?.Stream == null || r2Response.Stream.Length == 0)
                {
                    return NotFound("Image content is empty.");
                }

                return File(r2Response.Stream, r2Response.ContentType);
            }
            catch (Exception)
            {
                return NotFound("Product image could not be retrieved from R2 storage.");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ProductDto>> EditProduct(Guid id, [FromForm] CreateProductDto request)
        {
            var existingProduct = await _context.Products.FindAsync(id);

            if (existingProduct is null)
            {
                return NotFound("Product does not exist.");
            }

            if (request.Image != null && request.Image.Length > 0)
            {
                try
                {
                    // Deletes old image from R2 before uploading new one
                    if (!string.IsNullOrEmpty(existingProduct.Url))
                    {
                        await _r2ImageService.DeleteImageAsync(existingProduct.Url);
                    }

                    existingProduct.Url = await _r2ImageService.UploadImageAsync(request.Image, "products");
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Image upload failed: {ex.Message}");
                }
            }

            existingProduct.Title = request.Title;
            existingProduct.Description = request.Description;
            existingProduct.Price = request.Price;
            existingProduct.Quantity = request.Quantity;

            await _context.SaveChangesAsync();

            return Ok(new ProductDto
            {
                Id = existingProduct.Id,
                Title = existingProduct.Title,
                Description = existingProduct.Description,
                Quantity = existingProduct.Quantity,
                Price = existingProduct.Price,
                Url = GetProductImageUrl(existingProduct.Id, existingProduct.Url)
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product is null)
            {
                return NotFound("Can't delete because this product does not exist.");
            }

            // Deletes the file object from R2 storage
            if (!string.IsNullOrEmpty(product.Url))
            {
                await _r2ImageService.DeleteImageAsync(product.Url);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok("Product and associated R2 storage asset deleted successfully.");
        }

        /// <summary>
        /// Returns the internal API streaming URL if present, or empty string.
        /// </summary>
        private string GetProductImageUrl(Guid productId, string rawUrl)
        {
            if (string.IsNullOrEmpty(rawUrl)) return string.Empty;
            return $"{Request.Scheme}://{Request.Host}/api/product/{productId}/image";
        }
    }
}