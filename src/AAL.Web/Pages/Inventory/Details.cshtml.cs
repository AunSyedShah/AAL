using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Pages.Inventory
{
    [Authorize]
    public class DetailsInventoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsInventoryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public InventoryItem InventoryItem { get; set; } = default!;
        public List<Order> RecentOrders { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .FirstOrDefaultAsync(m => m.InventoryItemId == id);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            InventoryItem = inventoryItem;

            // Get recent orders for this product in this warehouse
            RecentOrders = await _context.Orders
                .Where(o => o.WarehouseId == inventoryItem.WarehouseId)
                .Include(o => o.OrderItems)
                .Where(o => o.OrderItems.Any(oi => oi.ProductId == inventoryItem.ProductId))
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            return Page();
        }
    }
}
