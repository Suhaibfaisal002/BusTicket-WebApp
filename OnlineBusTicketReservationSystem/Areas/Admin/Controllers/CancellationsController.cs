using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;

namespace OnlineBusTicketReservationSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CancellationsController : AdminBaseController
    {
        public CancellationsController(ApplicationDbContext context) : base(context) { }

        public async Task<IActionResult> Index(string? refundStatus)
        {
            ViewBag.RefundStatus = refundStatus;

            var query = _context.Cancellations
                .Include(c => c.Booking)
                    .ThenInclude(b => b!.Schedule)
                    .ThenInclude(s => s!.Route)
                .Include(c => c.Booking)
                    .ThenInclude(b => b!.Schedule)
                    .ThenInclude(s => s!.Bus)
                .Include(c => c.Booking)
                    .ThenInclude(b => b!.BookedByUser)
                .AsQueryable();

            if (refundStatus == "pending")
                query = query.Where(c => c.RefundStatus == "Pending");
            else if (refundStatus == "processed")
                query = query.Where(c => c.RefundStatus == "Processed");

            var cancellations = await query
                .OrderByDescending(c => c.CancellationDate)
                .ToListAsync();

            ViewBag.TotalCount     = cancellations.Count;
            ViewBag.PendingCount   = cancellations.Count(c => c.RefundStatus == "Pending");
            ViewBag.ProcessedCount = cancellations.Count(c => c.RefundStatus == "Processed");
            ViewBag.TotalRefund    = cancellations.Sum(c => c.RefundAmount);

            return View(cancellations);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRefunded(int id)
        {
            var cancellation = await _context.Cancellations.FindAsync(id);
            if (cancellation == null) return NotFound();
            cancellation.RefundStatus = "Processed";
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Refund for Booking #{cancellation.BookingId} marked as Processed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRefunded()
        {
            var pending = await _context.Cancellations
                .Where(c => c.RefundStatus == "Pending")
                .ToListAsync();
            foreach (var c in pending) c.RefundStatus = "Processed";
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{pending.Count} pending refund(s) marked as processed.";
            return RedirectToAction(nameof(Index));
        }
    }
}
