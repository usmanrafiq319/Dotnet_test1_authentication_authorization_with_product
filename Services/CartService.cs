using Dotnet_test1_authentication_authorization_with_product.Data;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Models;
using Microsoft.EntityFrameworkCore;

namespace Dotnet_test1_authentication_authorization_with_product.Services
{
    public class CartService(UserDbContext context) : ICartService
    {

        UserDbContext _context = context;
        public async Task<CartDto?> AddCartItemAsync(Guid userId, AddCartItemDto cartItem)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == cartItem.ProductId);

            if (product is null || cartItem.Quantity < 0 || product.Quantity == 0 )
            {
                return null;
            }

            Cart? cart = await _context.Carts.FirstOrDefaultAsync(cart => cart.UserId == userId);
            
            if(cart is null)
            {
                cart = new Cart()
                {
                    UserId = userId
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

            }
            
            var oldCart = await _context.CartItems.FirstOrDefaultAsync( ci => ci.ProductId == cartItem.ProductId && ci.CartId == cart.Id );
            
            if (oldCart is not null)
            {

                int difference = cartItem.Quantity - oldCart.Quantity;
                if (product.Quantity - difference < 0)
                {
                    return null;
                }
                else
                {

                    product.Quantity = product.Quantity - difference;

                }
                oldCart.Quantity = cartItem.Quantity;
                if(oldCart.Quantity == 0)
                {

                    _context.CartItems.Remove(oldCart);
                
                }

            }
            else
            {
                if (product.Quantity - cartItem.Quantity < 0 || cartItem.Quantity == 0)
                {

                    return null;
                
                }

                CartItem newCartItem = new CartItem()
                {

                    CartId = cart.Id,
                    ProductId = product.Id,
                    Quantity= cartItem.Quantity,
                    Price = product.Price

                };

                _context.CartItems.Add(newCartItem);
                product.Quantity = product.Quantity - cartItem.Quantity;
            
            }
            
            await _context.SaveChangesAsync();
            var Updatedcart = await _context.Carts.Where(c=>c.UserId == userId).Select(c=>new CartDto
            {

                CartItems = _context.CartItems.Where(ci=>ci.CartId == c.Id).Select(ci=>new CartItemDto
                {
                    ProductId = ci.ProductId,
                    Title = ci.Product.Title,
                    Price = ci.Product.Price,
                    Url=ci.Product.Url,
                    Quantity = ci.Quantity,
                }
                ).ToList(),
                Total = _context.CartItems.Where(ci=>ci.CartId == c.Id).Sum(ci=>ci.Product.Price*ci.Quantity)

            }).FirstOrDefaultAsync();
            return Updatedcart;
            
        }
        
        public async Task<CartDto?> GetCartAsync(Guid userId)
        {
            var Updatedcart = await _context.Carts.Where(c => c.UserId == userId).Select(c => new CartDto
            {

                CartItems = _context.CartItems.Where(ci => ci.CartId == c.Id).Select(ci => new CartItemDto
                {
                    ProductId = ci.ProductId,
                    Title = ci.Product.Title,
                    Price = ci.Product.Price,
                    Url = ci.Product.Url,
                    Quantity = ci.Quantity,
                }
    ).ToList(),
                Total = _context.CartItems.Where(ci => ci.CartId == c.Id).Sum(ci => ci.Product.Price * ci.Quantity)

            }).FirstOrDefaultAsync();
            return Updatedcart;
        }

        public async Task<int?> GetCartItemQuanityAsync(Guid userId, Guid productId)
        {
            var num = await _context.CartItems.Where(
                 ci => ci.Cart.UserId == userId && ci.ProductId == productId
                ).Select(ci => (int?)ci.Quantity).FirstOrDefaultAsync() ??0;
            return num;

        }

    }
}
