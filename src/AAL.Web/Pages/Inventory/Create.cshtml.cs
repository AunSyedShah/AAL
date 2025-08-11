using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Pages.Inventory
{
    [Authorize]
    public class CreateInventoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateInventoryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InventoryItem InventoryItem { get; set; } = new();

        public SelectList ProductSelectList { get; set; } = null!;
        public SelectList WarehouseSelectList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return Page();
            }

            // Check if inventory item already exists for this product and warehouse
            var existingItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.ProductId == InventoryItem.ProductId && i.WarehouseId == InventoryItem.WarehouseId);

            if (existingItem != null)
            {
                ModelState.AddModelError("", "An inventory item for this product already exists in the selected warehouse. Please edit the existing item instead.");
                await LoadSelectLists();
                return Page();
            }

            InventoryItem.LastUpdated = DateTime.UtcNow;
            _context.InventoryItems.Add(InventoryItem);
            await _context.SaveChangesAsync();

            return RedirectToPage("../Inventory");
        }

        private async Task LoadSelectLists()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .Select(p => new { p.ProductId, Name = $"{p.ProductCode} - {p.ProductName}" })
                .ToListAsync();

            ProductSelectList = new SelectList(products, "ProductId", "Name");

            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive)
                .ToListAsync();

            WarehouseSelectList = new SelectList(warehouses, "WarehouseId", "Name");
        }
    }
}
