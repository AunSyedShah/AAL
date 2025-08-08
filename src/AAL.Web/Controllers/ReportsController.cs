using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;
using System.Text;

namespace AAL.Web.Controllers
{
    [Authorize(Roles = "Admin,Manager,Finance")]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportReport(
            string type,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? customerRating = null,
            int? warehouseId = null)
        {
            try
            {
                dateFrom ??= DateTime.Now.AddMonths(-3);
                dateTo ??= DateTime.Now;

                var csvContent = type.ToLower() switch
                {
                    "sales" => await GenerateSalesReportCsv(dateFrom.Value, dateTo.Value, customerRating, warehouseId),
                    "inventory" => await GenerateInventoryReportCsv(warehouseId),
                    "financial" => await GenerateFinancialReportCsv(dateFrom.Value, dateTo.Value, customerRating),
                    "rejections" => await GenerateRejectionsReportCsv(dateFrom.Value, dateTo.Value),
                    _ => throw new ArgumentException("Invalid report type")
                };

                var fileName = $"{type}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var bytes = Encoding.UTF8.GetBytes(csvContent);

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateRejectionStatus(int id, [FromBody] UpdateRejectionStatusRequest request)
        {
            try
            {
                var rejection = await _context.MaterialRejections.FindAsync(id);
                if (rejection == null)
                {
                    return NotFound(new { success = false, message = "Material rejection not found" });
                }

                if (Enum.TryParse<RejectionStatus>(request.Status, out var newStatus))
                {
                    rejection.Status = newStatus;
                    // rejection.LastUpdated = DateTime.UtcNow; // Property doesn't exist
                    
                    await _context.SaveChangesAsync();
                    
                    return Ok(new { success = true, message = "Status updated successfully" });
                }
                
                return BadRequest(new { success = false, message = "Invalid status value" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateSalesReportCsv(DateTime dateFrom, DateTime dateTo, string? customerRating, int? warehouseId)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Warehouse)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderDate >= dateFrom && o.OrderDate <= dateTo);

            if (!string.IsNullOrEmpty(customerRating))
                ordersQuery = ordersQuery.Where(o => o.Customer.Rating.ToString() == customerRating);
            
            if (warehouseId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.WarehouseId == warehouseId.Value);

            var orders = await ordersQuery.ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Order ID,Customer,Customer Rating,Warehouse,Order Date,Status,Total Amount,Items Count,Payment Method");

            foreach (var order in orders)
            {
                csv.AppendLine($"{order.OrderId}," +
                              $"\"{order.Customer?.CompanyName ?? "Unknown"}\"," +
                              $"{order.Customer?.Rating ?? CustomerRating.Regular}," +
                              $"\"{order.Warehouse?.Name ?? "Unknown"}\"," +
                              $"{order.OrderDate:yyyy-MM-dd}," +
                              $"{order.Status}," +
                              $"{order.TotalAmount:F2}," +
                              $"{order.OrderItems?.Count ?? 0}," +
                              $"BankTransfer"); // Default payment method
            }

            return csv.ToString();
        }

        private async Task<string> GenerateInventoryReportCsv(int? warehouseId)
        {
            var inventoryQuery = _context.InventoryItems
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.Product.IsActive && i.Warehouse.IsActive);

            if (warehouseId.HasValue)
                inventoryQuery = inventoryQuery.Where(i => i.WarehouseId == warehouseId.Value);

            var inventory = await inventoryQuery.ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Product Name,Category,Warehouse,Stock Level,Reorder Point,EOQ,Movement Type,Last Updated");

            foreach (var item in inventory)
            {
                csv.AppendLine($"\"{item.Product.Name}\"," +
                              $"{item.Product.Category}," +
                              $"\"{item.Warehouse.Name}\"," +
                              $"{item.QuantityInStock}," +
                              $"{item.ReorderPoint}," +
                              $"{item.EconomicOrderQuantity}," +
                              $"{item.MovementType}," +
                              $"{item.LastUpdated:yyyy-MM-dd HH:mm}");
            }

            return csv.ToString();
        }

        private async Task<string> GenerateFinancialReportCsv(DateTime dateFrom, DateTime dateTo, string? customerRating)
        {
            var invoicesQuery = _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Order)
                .Where(i => i.InvoiceDate >= dateFrom && i.InvoiceDate <= dateTo);

            if (!string.IsNullOrEmpty(customerRating))
                invoicesQuery = invoicesQuery.Where(i => i.Customer.Rating.ToString() == customerRating);

            var invoices = await invoicesQuery.ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Invoice Number,Customer,Customer Rating,Invoice Date,Due Date,Sub Total,Tax Amount,Discount Amount,Total Amount,Amount Paid,Outstanding,Status");

            foreach (var invoice in invoices)
            {
                csv.AppendLine($"{invoice.InvoiceNumber}," +
                              $"\"{invoice.Customer.CompanyName}\"," +
                              $"{invoice.Customer.Rating}," +
                              $"{invoice.InvoiceDate:yyyy-MM-dd}," +
                              $"{invoice.DueDate:yyyy-MM-dd}," +
                              $"{invoice.SubTotal:F2}," +
                              $"{invoice.TaxAmount:F2}," +
                              $"{invoice.DiscountAmount:F2}," +
                              $"{invoice.TotalAmount:F2}," +
                              $"{invoice.AmountPaid:F2}," +
                              $"{invoice.OutstandingAmount:F2}," +
                              $"{invoice.Status}");
            }

            return csv.ToString();
        }

        private async Task<string> GenerateRejectionsReportCsv(DateTime dateFrom, DateTime dateTo)
        {
            var rejections = await _context.MaterialRejections
                .Where(mr => mr.RejectionDate >= dateFrom && mr.RejectionDate <= dateTo)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Rejection ID,Product Name,Supplier,Rejection Reason,Rejected Quantity,Cost Impact,Rejection Date,Status,Action Taken,Last Updated");

            foreach (var rejection in rejections)
            {
                csv.AppendLine($"REJ-{rejection.RejectionId:D6}," +
                              $"\"{rejection.Product?.Name ?? "Unknown"}\"," +
                              $"\"Unknown Supplier\"," + // Supplier info not in current model
                              $"\"{rejection.Reason}\"," +
                              $"{rejection.QuantityRejected}," +
                              $"{rejection.CostImpact ?? 0:F2}," +
                              $"{rejection.RejectionDate:yyyy-MM-dd}," +
                              $"{rejection.Status}," +
                              $"\"{rejection.ResolutionNotes ?? ""}\"," +
                              $"{rejection.CreatedDate:yyyy-MM-dd HH:mm}");
            }

            return csv.ToString();
        }
    }

    public class UpdateRejectionStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
