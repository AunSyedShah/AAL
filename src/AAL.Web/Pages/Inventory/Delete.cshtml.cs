using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Pages.Inventory
{
    [Authorize]
    public class DeleteInventoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteInventoryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InventoryItem InventoryItem { get; set; } = default!;

        public bool HasActiveOrders { get; set; }

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

            // Check if there are any active orders for this product in this warehouse
            HasActiveOrders = await _context.Orders
                .Where(o => o.WarehouseId == inventoryItem.WarehouseId)
                .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
                .Include(o => o.OrderItems)
                .AnyAsync(o => o.OrderItems.Any(oi => oi.ProductId == inventoryItem.ProductId));

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems.FindAsync(id);

            if (inventoryItem != null)
            {
                InventoryItem = inventoryItem;
                _context.InventoryItems.Remove(InventoryItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("../Inventory");
        }
    }
}
