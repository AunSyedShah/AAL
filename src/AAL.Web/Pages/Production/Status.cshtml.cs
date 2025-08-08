using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Pages.Production
{
    [Authorize]
    public class ProductionStatusModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProductionStatusModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ManufacturingPlant> ManufacturingPlants { get; set; } = new();
        public List<ProductProductionStatus> ProductionStatus { get; set; } = new();
        
        public int ActiveProducts { get; set; }
        public int InProductionCount { get; set; }
        public int PendingProductionCount { get; set; }
        public int ProductionCapacity { get; set; }

        public async Task OnGetAsync()
        {
            // Load manufacturing plants (mock data for demo)
            ManufacturingPlants = new List<ManufacturingPlant>
            {
                new() { 
                    Name = "Plant A - Engine Components", 
                    Location = "Birmingham", 
                    IsOperational = true, 
                    CurrentCapacity = 85, 
                    ActiveOrders = 12 
                },
                new() { 
                    Name = "Plant B - Brake Systems", 
                    Location = "Manchester", 
                    IsOperational = true, 
                    CurrentCapacity = 72, 
                    ActiveOrders = 8 
                },
                new() { 
                    Name = "Plant C - Filters & Spark Plugs", 
                    Location = "Glasgow", 
                    IsOperational = true, 
                    CurrentCapacity = 90, 
                    ActiveOrders = 15 
                },
                new() { 
                    Name = "Plant D - Tires & Accessories", 
                    Location = "London", 
                    IsOperational = false, 
                    CurrentCapacity = 0, 
                    ActiveOrders = 0 
                }
            };

            // Load products with inventory data
            var products = await _context.Products
                .Include(p => p.InventoryItems)
                .ThenInclude(ii => ii.Warehouse)
                .Where(p => p.IsActive)
                .ToListAsync();

            // Create production status for each product
            ProductionStatus = products.Select(p => new ProductProductionStatus
            {
                ProductId = p.ProductId,
                Product = p,
                Status = GenerateProductionStatus(p),
                CurrentStock = p.InventoryItems.Sum(ii => ii.QuantityInStock),
                QuantityInProduction = GenerateInProductionQuantity(p),
                EstimatedCompletion = GenerateEstimatedCompletion(p),
                PriorityOrders = GeneratePriorityOrdersCount(p.ProductId)
            }).ToList();

            // Calculate metrics
            ActiveProducts = products.Count;
            InProductionCount = ProductionStatus.Count(ps => ps.Status == Models.ProductionStatus.InProduction);
            PendingProductionCount = ProductionStatus.Count(ps => ps.Status == Models.ProductionStatus.Scheduled);
            ProductionCapacity = (int)ManufacturingPlants.Where(mp => mp.IsOperational).Average(mp => mp.CurrentCapacity);
        }

        private Models.ProductionStatus GenerateProductionStatus(Product product)
        {
            var totalStock = product.InventoryItems.Sum(ii => ii.QuantityInStock);
            var totalReorderPoint = product.InventoryItems.Sum(ii => ii.ReorderPoint);

            return totalStock <= totalReorderPoint ? Models.ProductionStatus.InProduction : Models.ProductionStatus.Scheduled;
        }

        private int GenerateInProductionQuantity(Product product)
        {
            var random = new Random(product.ProductId);
            return product.InventoryItems.Sum(ii => ii.QuantityInStock) <= product.InventoryItems.Sum(ii => ii.ReorderPoint) 
                ? random.Next(50, 200) 
                : 0;
        }

        private DateTime? GenerateEstimatedCompletion(Product product)
        {
            var status = GenerateProductionStatus(product);
            return status == Models.ProductionStatus.InProduction 
                ? DateTime.UtcNow.AddDays(new Random(product.ProductId).Next(3, 14))
                : null;
        }

        private int GeneratePriorityOrdersCount(int productId)
        {
            // In a real system, this would query actual urgent orders
            var random = new Random(productId);
            return random.Next(0, 5);
        }
    }

    public class ManufacturingPlant
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsOperational { get; set; }
        public int CurrentCapacity { get; set; }
        public int ActiveOrders { get; set; }
    }

    public class ProductProductionStatus
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public Models.ProductionStatus Status { get; set; }
        public int CurrentStock { get; set; }
        public int QuantityInProduction { get; set; }
        public DateTime? EstimatedCompletion { get; set; }
        public int PriorityOrders { get; set; }
    }
}

namespace AAL.Web.Models
{
    public enum ProductionStatus
    {
        Scheduled,
        InProduction,
        Delayed,
        OnHold,
        Completed
    }
}
