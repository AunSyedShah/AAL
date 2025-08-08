using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Pages
{
    public class InventoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public InventoryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<InventoryItem> InventoryItems { get; set; } = default!;
        public IList<Warehouse> Warehouses { get; set; } = default!;
        public int TotalProducts { get; set; }
        public int LowStockItems { get; set; }

        public async Task OnGetAsync()
        {
            InventoryItems = await _context.InventoryItems
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .OrderBy(i => i.Warehouse.Name)
                .ThenBy(i => i.Product.ProductName)
                .ToListAsync();

            Warehouses = await _context.Warehouses
                .Include(w => w.InventoryItems)
                .ThenInclude(i => i.Product)
                .Where(w => w.IsActive)
                .ToListAsync();

            TotalProducts = await _context.Products.CountAsync(p => p.IsActive);
            LowStockItems = InventoryItems.Count(i => i.QuantityInStock <= i.ReorderPoint);
        }
    }
}
