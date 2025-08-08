using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;

namespace AAL.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ApplicationDbContext _context;

    public IndexModel(ILogger<IndexModel> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public int TotalProducts { get; set; }
    public int TotalWarehouses { get; set; }
    public int LowStockItems { get; set; }
    public int TotalCustomers { get; set; }

    public async Task OnGetAsync()
    {
        TotalProducts = await _context.Products.CountAsync(p => p.IsActive);
        TotalWarehouses = await _context.Warehouses.CountAsync(w => w.IsActive);
        TotalCustomers = await _context.Users.CountAsync();
        
        var inventoryItems = await _context.InventoryItems.ToListAsync();
        LowStockItems = inventoryItems.Count(i => i.QuantityInStock <= i.ReorderPoint);
    }
}
