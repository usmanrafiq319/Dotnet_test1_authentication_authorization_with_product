using Microsoft.EntityFrameworkCore;
using Dotnet_test1_authentication_authorization_with_product.Data;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Models;
using Dotnet_test1_authentication_authorization_with_product.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dotnet_test1_authentication_authorization_with_product.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class productController(UserDbContext context, IR2ImageService r2ImageService) : ControllerBase
    {
        private readonly UserDbContext _context = context;
        private readonly IR2ImageService _r2ImageService = r2ImageService;

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreatProduct([FromForm] CreateProductDto request)
        {
            if (await _context.Products.AnyAsync(item => item.Title == request.Title))
            {
                return BadRequest("Product title already exists.");
            }

            string imageUrl = string.Empty;

            // Upload image to Cloudflare R2 under "products" folder if provided
            if (request.Image != null && request.Image.Length > 0)
            {
                try
                {
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

            var productDto = new ProductDto
            {
                Id = saveProduct.Id,
                Title = saveProduct.Title,
                Description = saveProduct.Description,
                Quantity = saveProduct.Quantity,
                Price = saveProduct.Price,
                Url = saveProduct.Url
            };

            return Ok(productDto);
        }

        [HttpGet]
        public async Task<ActionResult<List<ProductDto>>> GetAllProduct()
        {
            var list = await _context.Products.ToListAsync();

            if (list is null || list.Count == 0)
            {
                return BadRequest("No items found.");
            }

            return Ok(list);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProductDto>> GetSingleProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product is null)
            {
                return BadRequest("The product with this ID does not exist.");
            }

            return Ok(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product is null)
            {
                return BadRequest("Can't delete because this product does not exist.");
            }

            // Remove image from Cloudflare R2 if present
            if (!string.IsNullOrEmpty(product.Url))
            {
                await _r2ImageService.DeleteImageAsync(product.Url);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok("Product deleted successfully.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ProductDto>> EditProduct(Guid id, [FromForm] CreateProductDto request)
        {
            var existingProduct = await _context.Products.FindAsync(id);

            if (existingProduct is null)
            {
                return BadRequest("Product does not exist.");
            }

            // Handle optional image update
            if (request.Image != null && request.Image.Length > 0)
            {
                try
                {
                    // Delete the old image from R2 if it exists
                    if (!string.IsNullOrEmpty(existingProduct.Url))
                    {
                        await _r2ImageService.DeleteImageAsync(existingProduct.Url);
                    }

                    // Upload the new replacement image
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

            var updatedDto = new ProductDto
            {
                Id = existingProduct.Id,
                Title = existingProduct.Title,
                Description = existingProduct.Description,
                Quantity = existingProduct.Quantity,
                Price = existingProduct.Price,
                Url = existingProduct.Url
            };

            return Ok(updatedDto);
        }
    }
}