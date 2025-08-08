using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Pages
{
    [Authorize]
    public class CustomerPortalModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Customer> _userManager;

        public CustomerPortalModel(ApplicationDbContext context, UserManager<Customer> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Customer CurrentCustomer { get; set; } = null!;
        public List<Order> RecentOrders { get; set; } = new();
        public List<InventoryItem> InventoryStatus { get; set; } = new();
        
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalOrderValue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal AvailableCredit => CurrentCustomer.CreditLimit - CurrentCustomer.OutstandingBalance;
        public int PaymentRating { get; set; }

        public async Task OnGetAsync()
        {
            // Redirect to login if not authenticated
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return;
            }

            CurrentCustomer = await _userManager.GetUserAsync(User) ?? new Customer();
            
            if (string.IsNullOrEmpty(CurrentCustomer.Id))
            {
                // User not found, redirect to registration
                return;
            }

            // Load recent orders
            RecentOrders = await _context.Orders
                .Where(o => o.CustomerId == CurrentCustomer.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // Load inventory status based on customer rating
            var inventoryQuery = _context.InventoryItems
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.Product.IsActive && i.Warehouse.IsActive);

            // Premium customers can see all warehouse inventory
            if (CurrentCustomer.Rating == CustomerRating.VIP || CurrentCustomer.Rating == CustomerRating.Premium)
            {
                InventoryStatus = await inventoryQuery.ToListAsync();
            }
            else
            {
                // Regular customers see limited inventory
                InventoryStatus = await inventoryQuery
                    .Where(i => i.QuantityInStock > 0)
                    .ToListAsync();
            }

            // Calculate statistics
            var allOrders = await _context.Orders
                .Where(o => o.CustomerId == CurrentCustomer.Id)
                .ToListAsync();

            TotalOrders = allOrders.Count;
            CompletedOrders = allOrders.Count(o => o.Status == OrderStatus.Delivered);
            PendingOrders = allOrders.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing);
            TotalOrderValue = allOrders.Sum(o => o.TotalAmount);

            // Calculate payment rating (1-5 stars based on payment history)
            var paymentHistory = await _context.Payments
                .Where(p => p.CustomerId == CurrentCustomer.Id)
                .ToListAsync();

            TotalPaid = paymentHistory.Sum(p => p.Amount);

            if (CurrentCustomer.TotalOrders > 0)
            {
                var defaultRate = (double)CurrentCustomer.DefaultedPayments / CurrentCustomer.TotalOrders;
                PaymentRating = defaultRate switch
                {
                    <= 0.05 => 5, // Less than 5% defaults - 5 stars
                    <= 0.10 => 4, // Less than 10% defaults - 4 stars
                    <= 0.20 => 3, // Less than 20% defaults - 3 stars
                    <= 0.30 => 2, // Less than 30% defaults - 2 stars
                    _ => 1        // More than 30% defaults - 1 star
                };
            }
            else
            {
                PaymentRating = 3; // Default rating for new customers
            }
        }
    }
}
