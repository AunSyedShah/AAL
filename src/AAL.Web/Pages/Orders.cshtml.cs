using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Pages
{
    [Authorize]
    public class OrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Customer> _userManager;

        public OrdersModel(ApplicationDbContext context, UserManager<Customer> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Order> Orders { get; set; } = new();
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int RejectedOrders { get; set; }

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return;
            }

            // For admins and managers, show all orders
            // For customers, show only their orders
            IQueryable<Order> ordersQuery = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Warehouse)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product);

            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                // Show all orders for admin/manager
                Orders = await ordersQuery
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            else
            {
                // Show only customer's own orders
                Orders = await ordersQuery
                    .Where(o => o.CustomerId == currentUser.Id)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }

            TotalOrders = Orders.Count;
            PendingOrders = Orders.Count(o => o.Status == OrderStatus.Pending);
            CompletedOrders = Orders.Count(o => o.Status == OrderStatus.Delivered);
            RejectedOrders = Orders.Count(o => o.Status == OrderStatus.Cancelled);
        }
    }
}
