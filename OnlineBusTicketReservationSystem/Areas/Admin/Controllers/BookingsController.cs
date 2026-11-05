using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;
using OnlineBusTicketReservationSystem.Services;

namespace OnlineBusTicketReservationSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;

        public BookingsController(ApplicationDbContext context, IEmailService email)
        {
            _context = context;
            _email = email;
        }

        public async Task<IActionResult> Index(string? status, string? search)
        {
            ViewBag.Status = status;
            ViewBag.Search = search;

            var query = _context.Bookings
                .Include(b => b.BookedByUser)
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Passengers)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.BookingStatus == status);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(b =>
                    b.CustomerName!.Contains(search) ||
                    b.CustomerPhone!.Contains(search) ||
                    b.BookedByUser.Email!.Contains(search));

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            ViewBag.TotalCount = await _context.Bookings.CountAsync();
            ViewBag.PendingCount = await _context.Bookings.CountAsync(b => b.BookingStatus == "Pending");
            ViewBag.ConfirmedCount = await _context.Bookings.CountAsync(b => b.BookingStatus == "Confirmed");
            ViewBag.CancelledCount = await _context.Bookings.CountAsync(b => b.BookingStatus == "Cancelled");

            return View(bookings);
        }

        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookedByUser)
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Passengers)
                .Include(b => b.Payment)
                .Include(b => b.Cancellation)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookedByUser)
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Passengers)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            booking.BookingStatus = "Confirmed";

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == id);
            if (payment != null) payment.PaymentStatus = "Paid";

            await _context.SaveChangesAsync();

            try
            {
                if (booking.BookedByUser?.Email != null)
                {
                    var route      = $"{booking.Schedule?.Route?.SourceCity} → {booking.Schedule?.Route?.DestinationCity}";
                    var departure  = booking.Schedule?.DepartureTime.ToString("dd MMM yyyy, HH:mm") ?? "-";
                    var busNumber  = booking.Schedule?.Bus?.BusCode ?? "-";
                    var passengers = booking.Passengers?.Count ?? 0;

                    await _email.SendAsync(
                        booking.BookedByUser.Email,
                        booking.BookedByUser.FullName ?? "Customer",
                        "Booking Confirmed! 🎉 — SRC Travel",
                        EmailTemplates.BookingConfirmed(
                            booking.CustomerName ?? "Customer",
                            booking.BookingId, route, departure,
                            busNumber, passengers, booking.TotalAmount));
                }
            }
            catch { }

            // ✅ AJAX request hai toh JSON return karo
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Ok(new { success = true, message = $"Booking #{id:D4} confirmed." });

            TempData["Success"] = $"Booking #{id:D4} confirmed successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> CancelView(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Passengers)
                .Include(b => b.BookedByUser)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.BookingStatus != "Cancelled");
            if (booking == null) return NotFound();

            ViewBag.AdminReasons = new List<string>
            {
                "Bus breakdown / technical issue",
                "Route suspended",
                "Schedule change",
                "Overbooking error",
                "Customer request (manual)",
                "Fraud / policy violation",
                "Other"
            };
            return View(booking);
        }

        // Admin Refund Processing
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRefunded(int id)
        {
            var cancellation = await _context.Cancellations.FindAsync(id);
            if (cancellation == null) return NotFound();
            cancellation.RefundStatus = "Processed";
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Refund for Booking #{cancellation.BookingId} marked as Processed.";
            return RedirectToAction(nameof(Details), new { id = cancellation.BookingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var booking = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Passengers)
                .Include(b => b.BookedByUser)
                .FirstOrDefaultAsync(b => b.BookingId == id &&
                    b.BookingStatus != "Cancelled");

            if (booking == null) return NotFound();

            if (!await _context.Cancellations.AnyAsync(c => c.BookingId == id))
            {
                _context.Cancellations.Add(new Cancellation
                {
                    BookingId = id,
                    CancellationDate = DateTime.Now,
                    DeductionPercent = 0,
                    DeductedAmount = 0,
                    RefundAmount = booking.TotalAmount,
                    RefundStatus = "Pending",
                    Reason = reason,
                    CancelledBy = "Admin"
                });
            }

            booking.BookingStatus = "Cancelled";
            if (booking.Schedule != null && booking.Passengers != null)
                booking.Schedule.AvailableSeats += booking.Passengers.Count;

            await _context.SaveChangesAsync();

            // Email to customer
            try
            {
                if (booking.BookedByUser?.Email != null)
                {
                    var route = $"{booking.Schedule?.Route?.SourceCity} → {booking.Schedule?.Route?.DestinationCity}";
                    await _email.SendAsync(
                        booking.BookedByUser.Email,
                        booking.BookedByUser.FullName ?? "Customer",
                        "Booking Cancelled by Admin — SRC Travel",
                        EmailTemplates.BookingCancelledByAdmin(
                            booking.CustomerName ?? "Customer",
                            booking.BookingId, route,
                            booking.TotalAmount, reason ?? "Not specified"));
                }
            }
            catch { }

            TempData["Success"] = $"Booking #{id:D4} cancelled. Full refund of Rs. {booking.TotalAmount:N0} issued.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ✅ AJAX — Live stats for dashboard
        [HttpGet]
        public async Task<IActionResult> LiveStats()
        {
            var stats = new
            {
                pending   = await _context.Bookings.CountAsync(b => b.BookingStatus == "Pending"),
                confirmed = await _context.Bookings.CountAsync(b => b.BookingStatus == "Confirmed"),
                cancelled = await _context.Bookings.CountAsync(b => b.BookingStatus == "Cancelled"),
                total     = await _context.Bookings.CountAsync(),
                revenue   = await _context.Bookings
                    .Where(b => b.BookingStatus == "Confirmed")
                    .SumAsync(b => (decimal?)b.TotalAmount) ?? 0
            };
            return Json(stats);
        }
    }
}