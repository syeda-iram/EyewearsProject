using EyewearsProject.Areas.Admin.Models;
using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.OrdersModule)]
    public class ReturnsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public ReturnsController(AppDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        // GET: /Admin/Returns
        public async Task<IActionResult> Index(string? status)
        {
            var query = _context.Returns
                .Include(r => r.Order)
                    .ThenInclude(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReturnStatus>(status, out var parsedStatus))
                query = query.Where(r => r.Status == parsedStatus);

            var returns = await query.OrderByDescending(r => r.ReturnRequestDate).ToListAsync();

            var list = returns.Select(r => new ReturnListItemViewModel
            {
                Id = r.Id,
                OrderNumber = r.Order.OrderNumber,
                CustomerEmail = r.Order.User.Email ?? "",
                ReturnRequestDate = r.ReturnRequestDate,
                Reason = r.Reason,
                Status = r.Status,
                RefundAmount = r.RefundAmount
            }).ToList();

            ViewBag.AllStatuses = Enum.GetNames(typeof(ReturnStatus));
            ViewBag.CurrentStatus = status;
            ViewBag.PendingCount = await _context.Returns.CountAsync(r => r.Status == ReturnStatus.Requested);
            ViewData["Title"] = "Returns";
            return View(list);
        }

        // GET: /Admin/Returns/Process/5
        public async Task<IActionResult> Process(int id)
        {
            var ret = await _context.Returns
                .Include(r => r.Order)
                    .ThenInclude(o => o.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ret == null) return NotFound();

            var model = new ReturnProcessViewModel
            {
                Id = ret.Id,
                OrderNumber = ret.Order.OrderNumber,
                CustomerEmail = ret.Order.User.Email ?? "",
                ReturnRequestDate = ret.ReturnRequestDate,
                Reason = ret.Reason,
                Status = ret.Status,
                RefundAmount = ret.RefundAmount,
                OrderGrandTotal = ret.Order.GrandTotal
            };

            ViewData["Title"] = $"Return for {ret.Order.OrderNumber}";
            return View(model);
        }

        // POST: /Admin/Returns/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, decimal refundAmount)
        {
            var ret = await _context.Returns.Include(r => r.Order).FirstOrDefaultAsync(r => r.Id == id);
            if (ret == null) return NotFound();

            ret.Status = ReturnStatus.Approved;
            ret.RefundAmount = refundAmount;
            ret.UpdatedAt = DateTime.UtcNow;
            ret.Order.OrderStatus = OrderStatus.Returned;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Return", ret.Id.ToString(),
                $"Approved return for order {ret.Order.OrderNumber}, refund {refundAmount:C}");

            TempData["Success"] = "Return approved.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Returns/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var ret = await _context.Returns.Include(r => r.Order).FirstOrDefaultAsync(r => r.Id == id);
            if (ret == null) return NotFound();

            ret.Status = ReturnStatus.Rejected;
            ret.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Return", ret.Id.ToString(), $"Rejected return for order {ret.Order.OrderNumber}");

            TempData["Success"] = "Return rejected.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Returns/MarkRefunded/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRefunded(int id)
        {
            var ret = await _context.Returns.Include(r => r.Order).ThenInclude(o => o.Payment).FirstOrDefaultAsync(r => r.Id == id);
            if (ret == null) return NotFound();

            ret.Status = ReturnStatus.Refunded;
            ret.ProcessedAt = DateTime.UtcNow;
            ret.UpdatedAt = DateTime.UtcNow;

            if (ret.Order.Payment != null)
                ret.Order.Payment.Status = PaymentStatus.Refunded;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Return", ret.Id.ToString(),
                $"Marked refunded for order {ret.Order.OrderNumber}, amount {ret.RefundAmount:C}");

            TempData["Success"] = "Refund recorded.";
            return RedirectToAction(nameof(Index));
        }
    }
}