using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Pages.Inventory
{
    [Authorize]
    public class EditInventoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditInventoryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InventoryItem InventoryItem { get; set; } = default!;

        public string ProductDisplay { get; set; } = string.Empty;
        public string WarehouseDisplay { get; set; } = string.Empty;

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
            ProductDisplay = $"{inventoryItem.Product.ProductCode} - {inventoryItem.Product.ProductName}";
            WarehouseDisplay = $"{inventoryItem.Warehouse.Name} - {inventoryItem.Warehouse.Location}";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Reload display data
                var inventoryItem = await _context.InventoryItems
                    .Include(i => i.Product)
                    .Include(i => i.Warehouse)
                    .FirstOrDefaultAsync(m => m.InventoryItemId == InventoryItem.InventoryItemId);

                if (inventoryItem != null)
                {
                    ProductDisplay = $"{inventoryItem.Product.ProductCode} - {inventoryItem.Product.ProductName}";
                    WarehouseDisplay = $"{inventoryItem.Warehouse.Name} - {inventoryItem.Warehouse.Location}";
                }

                return Page();
            }

            InventoryItem.LastUpdated = DateTime.UtcNow;
            _context.Attach(InventoryItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await InventoryItemExists(InventoryItem.InventoryItemId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("../Inventory");
        }

        private async Task<bool> InventoryItemExists(int id)
        {
            return await _context.InventoryItems.AnyAsync(e => e.InventoryItemId == id);
        }
    }
}
