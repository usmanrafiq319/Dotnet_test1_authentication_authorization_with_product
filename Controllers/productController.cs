using Microsoft.EntityFrameworkCore;
using Dotnet_test1_authentication_authorization_with_product.Data;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dotnet_test1_authentication_authorization_with_product.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class productController(UserDbContext context) : ControllerBase
    {
        private readonly UserDbContext _context = context;

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreatProduct(ProductDto product)
        {
            // Use AnyAsync to avoid blocking threads while checking existing products
            if (await _context.Products.AnyAsync(item => item.Title == product.Title))
            {
                return BadRequest("product title already exist");
            }

            var saveproduct = new Product
            {
                Title = product.Title,
                Description = product.Description,
                Quantity = product.Quantity,
                Price = product.Price,
                Url = product.Url
            };

            _context.Products.Add(saveproduct);

            // Await the asynchronous database save operation
            await _context.SaveChangesAsync();

            ProductDto productDto = new ProductDto()
            {
                Id = saveproduct.Id,
                Title = product.Title,
                Description = product.Description,
                Quantity = product.Quantity,
                Price = product.Price,
                Url = product.Url
            };

            return Ok(productDto);
        }

        [HttpGet]
        public async Task<ActionResult<List<ProductDto>>> GetAllProduct()
        {
            // Use ToListAsync to fetch the dataset asynchronously
            var list = await _context.Products.ToListAsync();

            if (list is null || list.Count == 0)
            {
                return BadRequest("no item found");
            }

            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetSingleProduct(Guid id)
        {
            // Use FindAsync for primary key lookups asynchronously
            var product = await _context.Products.FindAsync(id);

            if (product is null)
            {
                return BadRequest("the product with this id doesnt exist");
            }

            return Ok(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            // Note: If your Product entity uses a Guid primary key, you should change 'int id' to 'Guid id'
            var deleted = await _context.Products.FindAsync(id);

            if (deleted is null)
            {
                return BadRequest("can't delete casue this product dont exists");
            }

            _context.Products.Remove(deleted);
            await _context.SaveChangesAsync();

            return Ok("product deleted successfully");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<ProductDto>> EditProduct(int id, ProductDto request)
        {
            // Note: If your Product entity uses a Guid primary key, you should change 'int id' to 'Guid id'
            var updatedProduct = await _context.Products.FindAsync(id);

            if (updatedProduct is null)
            {
                return BadRequest("product dont exist");
            }

            updatedProduct.Title = request.Title;
            updatedProduct.Url = request.Url;
            updatedProduct.Description = request.Description;
            updatedProduct.Price = request.Price;
            updatedProduct.Quantity = request.Quantity;

            await _context.SaveChangesAsync();

            return Ok(updatedProduct);
        }
    }

}