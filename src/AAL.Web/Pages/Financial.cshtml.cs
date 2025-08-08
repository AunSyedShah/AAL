using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Pages
{
    [Authorize(Roles = "Admin,Manager,Finance")]
    public class FinancialModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public FinancialModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Invoice> Invoices { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();
        public List<MaterialRejection> MaterialRejections { get; set; } = new();

        public decimal TotalRevenue { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public int OverdueInvoices { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Additional security check - redirect non-authorized users
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager") && !User.IsInRole("Finance"))
            {
                return Forbid();
            }

            await LoadFinancialDataAsync();
            return Page();
        }

        private async Task LoadFinancialDataAsync()
        {
            // Load financial data
            Invoices = await _context.Invoices
                .Include(i => i.Customer)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(50)
                .ToListAsync();

            Payments = await _context.Payments
                .Include(p => p.Customer)
                .OrderByDescending(p => p.PaymentDate)
                .Take(50)
                .ToListAsync();

            MaterialRejections = await _context.MaterialRejections
                .OrderByDescending(mr => mr.RejectionDate)
                .Take(50)
                .ToListAsync();

            // Calculate summary statistics
            TotalRevenue = await _context.Invoices.SumAsync(i => i.TotalAmount);
            OutstandingAmount = await _context.Invoices.Where(i => i.Status != InvoiceStatus.Paid).SumAsync(i => i.OutstandingAmount);
            PaidAmount = await _context.Payments.SumAsync(p => p.Amount);
            OverdueInvoices = await _context.Invoices
                .CountAsync(i => i.Status != InvoiceStatus.Paid && i.DueDate < DateTime.Now);
        }
    }
}
