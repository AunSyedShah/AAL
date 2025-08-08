using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Customer> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<Customer> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("{id}/process")]
        public async Task<IActionResult> ProcessOrder(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound(new { success = false, message = "Order not found" });
                }

                // Only allow users to process their own orders, or admins/managers to process any order
                if (order.CustomerId != currentUser.Id && 
                    !User.IsInRole("Admin") && 
                    !User.IsInRole("Manager"))
                {
                    return Forbid();
                }

                if (order.Status != OrderStatus.Pending)
                {
                    return BadRequest(new { success = false, message = "Order is not in pending status" });
                }

                // Validate credit limit
                if (order.Customer.OutstandingBalance + order.TotalAmount > order.Customer.CreditLimit)
                {
                    return BadRequest(new { success = false, message = "Order exceeds customer credit limit" });
                }

                // Update order status
                order.Status = OrderStatus.Confirmed;
                order.ConfirmedDate = DateTime.UtcNow;

                // Update customer outstanding balance
                order.Customer.OutstandingBalance += order.TotalAmount;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Order processed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error processing order: {ex.Message}" });
            }
        }

        [HttpGet("{id}/track")]
        public async Task<IActionResult> TrackOrder(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Warehouse)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound(new { success = false, message = "Order not found" });
                }

                // Only allow users to track their own orders, or admins/managers to track any order
                if (order.CustomerId != currentUser.Id && 
                    !User.IsInRole("Admin") && 
                    !User.IsInRole("Manager"))
                {
                    return Forbid();
                }

                var trackingInfo = new
                {
                    orderNumber = order.OrderNumber,
                    status = order.Status.ToString(),
                    orderDate = order.OrderDate,
                    estimatedDelivery = order.RequiredDate,
                    warehouse = order.Warehouse?.Name,
                    items = order.OrderItems.Select(oi => new
                    {
                        product = oi.Product.Name,
                        quantity = oi.Quantity,
                        unitPrice = oi.UnitPrice
                    }),
                    timeline = new object[]
                    {
                        new { stage = "Order Placed", date = order.OrderDate, completed = true },
                        new { stage = "Order Confirmed", date = order.ConfirmedDate, completed = order.Status >= OrderStatus.Confirmed },
                        new { stage = "In Production", date = (DateTime?)null, completed = order.Status >= OrderStatus.Processing },
                        new { stage = "Shipped", date = order.ShippedDate, completed = order.Status >= OrderStatus.Shipped },
                        new { stage = "Delivered", date = order.DeliveredDate, completed = order.Status == OrderStatus.Delivered }
                    }
                };

                return Ok(new { success = true, data = trackingInfo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error tracking order: {ex.Message}" });
            }
        }
    }
}
