using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Pages.Orders
{
    [Authorize]
    public class CreateOrderModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateOrderModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Order Order { get; set; } = new();

        [BindProperty]
        public List<OrderItem> OrderItems { get; set; } = new();

        public SelectList CustomerSelectList { get; set; } = null!;
        public SelectList WarehouseSelectList { get; set; } = null!;
        public List<Product> Products { get; set; } = new();

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

            // Generate order number
            var lastOrder = await _context.Orders
                .OrderByDescending(o => o.OrderId)
                .FirstOrDefaultAsync();
            
            var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{(lastOrder?.OrderId + 1 ?? 1):D4}";
            
            Order.OrderNumber = orderNumber;
            Order.OrderDate = DateTime.UtcNow;
            Order.Status = OrderStatus.Pending;

            // Validate customer credit limit
            var customer = await _context.Users.FindAsync(Order.CustomerId);
            if (customer != null)
            {
                var orderTotal = OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);
                if (customer.OutstandingBalance + orderTotal > customer.CreditLimit)
                {
                    ModelState.AddModelError("", $"Order total exceeds customer credit limit. Available credit: ${(customer.CreditLimit - customer.OutstandingBalance):N2}");
                    await LoadSelectLists();
                    return Page();
                }
                Order.TotalAmount = orderTotal;
            }

            // Validate stock availability
            foreach (var item in OrderItems.Where(oi => oi.ProductId > 0))
            {
                var availableStock = await _context.InventoryItems
                    .Where(ii => ii.ProductId == item.ProductId && ii.WarehouseId == Order.WarehouseId)
                    .SumAsync(ii => ii.QuantityInStock);

                if (item.Quantity > availableStock)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    ModelState.AddModelError("", $"Insufficient stock for {product?.Name}. Available: {availableStock}, Requested: {item.Quantity}");
                    await LoadSelectLists();
                    return Page();
                }
            }

            _context.Orders.Add(Order);
            await _context.SaveChangesAsync();

            // Add order items
            foreach (var item in OrderItems.Where(oi => oi.ProductId > 0))
            {
                item.OrderId = Order.OrderId;
                item.TotalPrice = item.Quantity * item.UnitPrice;
                _context.OrderItems.Add(item);
            }

            await _context.SaveChangesAsync();

            // Update inventory
            await UpdateInventoryAsync();

            // Generate invoice
            await GenerateInvoiceAsync();

            return RedirectToPage("/Orders");
        }

        private async Task LoadSelectLists()
        {
            var customers = await _context.Users
                .Where(c => c.EmailConfirmed)
                .Select(c => new { c.Id, Name = $"{c.FirstName} {c.LastName} ({c.CompanyName})" })
                .ToListAsync();

            CustomerSelectList = new SelectList(customers, "Id", "Name");

            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive)
                .ToListAsync();

            WarehouseSelectList = new SelectList(warehouses, "WarehouseId", "Name");

            Products = await _context.Products
                .Include(p => p.InventoryItems)
                .Where(p => p.IsActive)
                .ToListAsync();
        }

        private async Task UpdateInventoryAsync()
        {
            foreach (var item in OrderItems.Where(oi => oi.ProductId > 0))
            {
                var inventoryItems = await _context.InventoryItems
                    .Where(ii => ii.ProductId == item.ProductId && ii.WarehouseId == Order.WarehouseId)
                    .ToListAsync();

                var remainingQuantity = item.Quantity;
                foreach (var inventoryItem in inventoryItems)
                {
                    if (remainingQuantity <= 0) break;

                    var deductQuantity = Math.Min(remainingQuantity, inventoryItem.QuantityInStock);
                    inventoryItem.QuantityInStock -= deductQuantity;
                    inventoryItem.LastUpdated = DateTime.UtcNow;
                    remainingQuantity -= deductQuantity;
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task GenerateInvoiceAsync()
        {
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{Order.OrderId:D4}",
                OrderId = Order.OrderId,
                CustomerId = Order.CustomerId,
                InvoiceDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30), // 30 days payment terms
                SubTotal = Order.TotalAmount,
                TaxAmount = Order.TotalAmount * 0.1m, // 10% tax
                TotalAmount = Order.TotalAmount * 1.1m,
                Status = InvoiceStatus.Generated
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
        }
    }
}
