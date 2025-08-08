using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Pages
{
    [Authorize(Roles = "Admin,Manager,Finance")]
    public class ReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReportsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Query Parameters
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? CustomerRating { get; set; }
        public int? WarehouseId { get; set; }

        // Data Properties
        public List<Warehouse> AvailableWarehouses { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public int TotalRejections { get; set; }
        public decimal RejectionCost { get; set; }
        public double RejectionRate { get; set; }
        public double QualityScore { get; set; }

        // Chart Data
        public List<string> SalesChartLabels { get; set; } = new();
        public List<decimal> SalesChartData { get; set; } = new();
        public List<string> CustomerRatingLabels { get; set; } = new();
        public List<int> CustomerRatingData { get; set; } = new();

        // Report Data
        public List<TopProductReport> TopProducts { get; set; } = new();
        public List<InventoryMovementReport> InventoryMovementReport { get; set; } = new();
        public List<MaterialRejection> MaterialRejections { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(DateTime? dateFrom, DateTime? dateTo, string? customerRating, int? warehouseId)
        {
            DateFrom = dateFrom ?? DateTime.Now.AddMonths(-3);
            DateTo = dateTo ?? DateTime.Now;
            CustomerRating = customerRating;
            WarehouseId = warehouseId;

            await LoadReportDataAsync();
            return Page();
        }

        private async Task LoadReportDataAsync()
        {
            // Load available warehouses
            AvailableWarehouses = await _context.Warehouses
                .Where(w => w.IsActive)
                .ToListAsync();

            // Build query filters
            var ordersQuery = _context.Orders.AsQueryable();
            if (DateFrom.HasValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= DateFrom.Value);
            if (DateTo.HasValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate <= DateTo.Value);
            if (WarehouseId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.WarehouseId == WarehouseId.Value);

            var customersQuery = _context.Users.OfType<Customer>();
            if (!string.IsNullOrEmpty(CustomerRating))
                customersQuery = customersQuery.Where(c => c.Rating.ToString() == CustomerRating);

            // Financial Summary
            var invoices = await _context.Invoices
                .Where(i => DateFrom == null || i.InvoiceDate >= DateFrom)
                .Where(i => DateTo == null || i.InvoiceDate <= DateTo)
                .ToListAsync();

            TotalRevenue = invoices.Sum(i => i.TotalAmount);
            PaidAmount = invoices.Sum(i => i.AmountPaid);
            OutstandingAmount = invoices.Sum(i => i.OutstandingAmount);

            // Material Rejections Analysis
            var rejections = await _context.MaterialRejections
                .Where(mr => DateFrom == null || mr.RejectionDate >= DateFrom)
                .Where(mr => DateTo == null || mr.RejectionDate <= DateTo)
                .ToListAsync();

            TotalRejections = rejections.Count;
            RejectionCost = rejections.Sum(r => r.CostImpact ?? 0);
            
            var totalMaterialOrders = await _context.Orders
                .Where(o => DateFrom == null || o.OrderDate >= DateFrom)
                .Where(o => DateTo == null || o.OrderDate <= DateTo)
                .CountAsync();

            RejectionRate = totalMaterialOrders > 0 ? (double)TotalRejections / totalMaterialOrders : 0;
            QualityScore = Math.Max(0, 100 - (RejectionRate * 100));

            MaterialRejections = rejections.Take(10).ToList();

            // Sales Chart Data (Monthly)
            var monthlySales = await _context.Orders
                .Where(o => o.OrderDate >= (DateFrom ?? DateTime.Now.AddMonths(-12)))
                .Where(o => o.OrderDate <= (DateTo ?? DateTime.Now))
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalSales = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            SalesChartLabels = monthlySales.Select(s => $"{s.Year}-{s.Month:D2}").ToList();
            SalesChartData = monthlySales.Select(s => s.TotalSales).ToList();

            // Customer Rating Distribution
            var customerRatings = await customersQuery
                .GroupBy(c => c.Rating)
                .Select(g => new
                {
                    Rating = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            CustomerRatingLabels = customerRatings.Select(r => r.Rating).ToList();
            CustomerRatingData = customerRatings.Select(r => r.Count).ToList();

            // Top Products Analysis
            var productSales = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => DateFrom == null || oi.Order.OrderDate >= DateFrom)
                .Where(oi => DateTo == null || oi.Order.OrderDate <= DateTo)
                .GroupBy(oi => oi.Product)
                .Select(g => new TopProductReport
                {
                    ProductName = g.Key.Name,
                    Category = g.Key.Category.ToString(),
                    TotalOrders = g.Select(oi => oi.Order).Distinct().Count(),
                    TotalQuantity = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice),
                    MovementType = g.Sum(oi => oi.Quantity) > 100 ? "Fast" : "Slow"
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToListAsync();

            // Calculate performance percentage
            var maxRevenue = productSales.FirstOrDefault()?.Revenue ?? 1;
            foreach (var product in productSales)
            {
                product.PerformancePercentage = maxRevenue > 0 ? (int)((product.Revenue / maxRevenue) * 100) : 0;
            }

            TopProducts = productSales;

            // Inventory Movement Report
            var inventoryMovement = await _context.InventoryItems
                .Include(i => i.Product)
                .GroupBy(i => i.Product.Category)
                .Select(g => new InventoryMovementReport
                {
                    Category = g.Key.ToString(),
                    FastMoving = g.Count(i => i.MovementType == MovementType.Fast),
                    SlowMoving = g.Count(i => i.MovementType == MovementType.Slow),
                    Total = g.Count()
                })
                .ToListAsync();

            InventoryMovementReport = inventoryMovement;
        }
    }

    public class TopProductReport
    {
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int TotalQuantity { get; set; }
        public decimal Revenue { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public int PerformancePercentage { get; set; }
    }

    public class InventoryMovementReport
    {
        public string Category { get; set; } = string.Empty;
        public int FastMoving { get; set; }
        public int SlowMoving { get; set; }
        public int Total { get; set; }
    }
}
