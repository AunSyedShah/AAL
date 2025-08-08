using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Controllers
{
    [Authorize(Roles = "Admin,Manager,Finance")]
    [ApiController]
    [Route("api/[controller]")]
    public class RejectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RejectionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("{id}/resolve")]
        public async Task<IActionResult> ResolveRejection(int id)
        {
            try
            {
                var rejection = await _context.MaterialRejections
                    .FirstOrDefaultAsync(r => r.RejectionId == id);

                if (rejection == null)
                {
                    return NotFound(new { success = false, message = "Rejection not found" });
                }

                if (rejection.Status == RejectionStatus.Resolved)
                {
                    return BadRequest(new { success = false, message = "Rejection is already resolved" });
                }

                rejection.Status = RejectionStatus.Resolved;
                rejection.ResolutionDate = DateTime.UtcNow;
                rejection.ResolutionNotes = "Resolved via admin interface";

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Rejection resolved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error resolving rejection: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRejection([FromBody] CreateRejectionRequest request)
        {
            try
            {
                var rejection = new MaterialRejection
                {
                    RejectionNumber = $"REJ-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    CustomerId = request.CustomerId,
                    ProductId = request.ProductId,
                    RejectionDate = DateTime.UtcNow,
                    RejectedQuantity = request.RejectedQuantity,
                    Reason = request.Reason,
                    Description = request.Description,
                    Status = RejectionStatus.Reported
                };

                _context.MaterialRejections.Add(rejection);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, rejectionId = rejection.RejectionId, message = "Rejection reported successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error creating rejection: {ex.Message}" });
            }
        }

        [HttpGet("report")]
        public async Task<IActionResult> GetRejectionReport(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                var rejections = await _context.MaterialRejections
                    .Include(r => r.Customer)
                    .Include(r => r.Product)
                    .Where(r => r.RejectionDate >= start && r.RejectionDate <= end)
                    .ToListAsync();

                var report = new
                {
                    totalRejections = rejections.Count,
                    totalQuantity = rejections.Sum(r => r.RejectedQuantity),
                    byProduct = rejections.GroupBy(r => r.Product.Name)
                        .Select(g => new { product = g.Key, count = g.Count(), quantity = g.Sum(r => r.RejectedQuantity) }),
                    byCustomer = rejections.GroupBy(r => r.Customer.CompanyName)
                        .Select(g => new { customer = g.Key, count = g.Count(), quantity = g.Sum(r => r.RejectedQuantity) }),
                    byReason = rejections.GroupBy(r => r.Reason)
                        .Select(g => new { reason = g.Key, count = g.Count() }),
                    resolvedCount = rejections.Count(r => r.Status == RejectionStatus.Resolved),
                    pendingCount = rejections.Count(r => r.Status != RejectionStatus.Resolved)
                };

                return Ok(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error generating rejection report: {ex.Message}" });
            }
        }
    }

    public class CreateRejectionRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public int RejectedQuantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
