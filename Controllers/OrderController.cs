using Dotnet_test1_authentication_authorization_with_product.Data;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dotnet_test1_authentication_authorization_with_product.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly UserDbContext _context;

        public OrderController(UserDbContext context)
        {
            _context = context;
        }

        // POST: api/order
        [HttpPost]
        public async Task<ActionResult<CreateOrderResponseDto>> CreateOrder()
        {
            var userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new
                {
                    message = "Invalid user."
                });
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return BadRequest(new
                {
                    message = "Cart not found."
                });
            }

            if (cart.CartItems.Count == 0)
            {
                return BadRequest(new
                {
                    message = "Your cart is empty."
                });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    OrderTime = DateTime.UtcNow,
                    Status = OrderStatus.Pending
                };

                decimal totalAmount = 0;

                foreach (var cartItem in cart.CartItems)
                {
                    if (cartItem.Product == null)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message =
                                "One of the products in your cart no longer exists."
                        });
                    }

                    if (cartItem.Quantity <= 0)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = "Invalid product quantity."
                        });
                    }

                    if (cartItem.Price < 0)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = "Invalid product price."
                        });
                    }

                    var orderItem = new OrderItem
                    {

                        OrderId = order.Id,

                        ProductId = cartItem.ProductId,

                        Quantity = cartItem.Quantity,

                        UnitPrice = cartItem.Price
                    };

                    order.OrderItems.Add(orderItem);

                    totalAmount +=
                        (decimal)cartItem.Price * cartItem.Quantity;
                }

                order.TotalAmount = totalAmount;

                _context.Orders.Add(order);

                // Clear the cart after creating the order
                _context.CartItems.RemoveRange(cart.CartItems);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                var response = new CreateOrderResponseDto
                {
                    OrderId = order.Id,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status.ToString()
                };

                return Ok(response);
            }
            catch
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message =
                        "Something went wrong while creating the order."
                });
            }
        }


        // GET: api/order/my-orders
        [HttpGet("my-orders")]
        public async Task<ActionResult<List<OrderDto>>> GetMyOrders()
        {
            var userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new
                {
                    message = "Invalid user."
                });
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderTime)
                .Select(o => new OrderDto
                {
                    UserId = o.UserId,
                    OrderTime = o.OrderTime,
                    Status = o.Status.ToString(),
                    TotalAmount = o.TotalAmount,

                    OrderItems = o.OrderItems
                        .Select(oi => new OrderItemDto
                        {
                            ProductId = oi.ProductId,

                            ProductName = oi.Product != null
                                ? oi.Product.Title
                                : "Product unavailable",

                            Quantity = oi.Quantity,

                            UnitPrice = oi.UnitPrice,

                            SubTotal =
                                oi.UnitPrice * oi.Quantity
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }


        // GET: api/order/admin
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<OrderDto>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderTime)
                .Select(o => new OrderDto
                {
                    UserId = o.UserId,
                    OrderTime = o.OrderTime,
                    Status = o.Status.ToString(),
                    TotalAmount = o.TotalAmount,

                    OrderItems = o.OrderItems
                        .Select(oi => new OrderItemDto
                        {
                            ProductId = oi.ProductId,

                            ProductName = oi.Product != null
                                ? oi.Product.Title
                                : "Product unavailable",

                            Quantity = oi.Quantity,

                            UnitPrice = oi.UnitPrice,

                            SubTotal =
                                oi.UnitPrice * oi.Quantity
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }


        // PUT: api/order/admin/{orderId}/status
        [HttpPut("admin/{orderId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateOrderStatus(Guid orderId,UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound(new
                {
                    message = "Order not found."
                });
            }

            order.Status = dto.Status;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order status updated successfully.",
                orderId = order.Id,
                status = order.Status.ToString()
            });
        }
    }
}