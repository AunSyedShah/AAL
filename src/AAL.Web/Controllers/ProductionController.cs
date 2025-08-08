using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AAL.Web.Data;
using AAL.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AAL.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("details/{productId}")]
        public async Task<IActionResult> GetProductionDetails(int productId)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.InventoryItems)
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null)
                {
                    return NotFound(new { success = false, message = "Product not found" });
                }

                // Mock production timeline data
                var timeline = new[]
                {
                    new { stage = "Raw Material Procurement", status = "Completed" },
                    new { stage = "Manufacturing", status = "In Progress" },
                    new { stage = "Quality Control", status = "Pending" },
                    new { stage = "Packaging", status = "Pending" },
                    new { stage = "Warehouse Transfer", status = "Pending" }
                };

                // Mock quality metrics
                var random = new Random(productId);
                var productionData = new
                {
                    timeline = timeline,
                    defectRate = Math.Round(random.NextDouble() * 5, 2), // 0-5%
                    onTimeDelivery = random.Next(85, 98), // 85-98%
                    efficiency = random.Next(75, 95) // 75-95%
                };

                return Ok(productionData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error getting production details: {ex.Message}" });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetProductionStatus()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.InventoryItems)
                    .Where(p => p.IsActive)
                    .ToListAsync();

                var statusData = products.Select(p => new
                {
                    productId = p.ProductId,
                    productName = p.Name,
                    currentStock = p.InventoryItems.Sum(ii => ii.QuantityInStock),
                    inProduction = new Random(p.ProductId).Next(0, 100),
                    status = p.InventoryItems.Sum(ii => ii.QuantityInStock) > 100 ? "Adequate" : "Low Stock",
                    estimatedCompletion = DateTime.UtcNow.AddDays(new Random(p.ProductId).Next(1, 14))
                });

                return Ok(new { success = true, data = statusData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error getting production status: {ex.Message}" });
            }
        }
    }
}
