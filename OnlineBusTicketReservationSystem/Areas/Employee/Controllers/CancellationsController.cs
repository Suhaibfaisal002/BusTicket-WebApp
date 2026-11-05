using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class CancellationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public CancellationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var cancellations = await _context.Cancellations
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Schedule)
                    .ThenInclude(s => s.Route)
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Schedule)
                    .ThenInclude(s => s.Bus)
                .Where(c => c.Booking.BookedByUserId == userId)
                .OrderByDescending(c => c.CancellationDate)
                .ToListAsync();
            return View(cancellations);
        }

        public async Task<IActionResult> Cancel(int? bookingId)
        {
            if (bookingId == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Passengers)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            if (booking.BookingStatus == "Cancelled")
            {
                TempData["Error"] = "This booking is already cancelled.";
                return RedirectToAction("Index", "Bookings");
            }

            // Calculate refund preview
            var fs = _config.GetSection("FareSettings");
            int fullRefundDays = fs.GetValue<int>("CancelFullRefundDays", 2);
            decimal oneDayDed =
                fs.GetValue<decimal>("CancelOneDayDeductionPercent", 15);
            decimal sameDayDed =
                fs.GetValue<decimal>("CancelSameDayDeductionPercent", 30);

            var daysLeft =
                (booking.Schedule.DepartureTime.Date - DateTime.Today).Days;

            decimal deductionPercent;
            if (daysLeft >= fullRefundDays) deductionPercent = 0;
            else if (daysLeft == 1) deductionPercent = oneDayDed;
            else deductionPercent = sameDayDed;

            decimal deductedAmount =
                Math.Round(booking.TotalAmount * deductionPercent / 100, 2);
            decimal refundAmount = booking.TotalAmount - deductedAmount;

            ViewBag.DeductionPercent = deductionPercent;
            ViewBag.DeductedAmount = deductedAmount;
            ViewBag.RefundAmount = refundAmount;
            ViewBag.DaysLeft = daysLeft;

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCancel(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Schedule)
                .Include(b => b.Payment)
                .Include(b => b.Passengers)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();
            if (booking.BookingStatus == "Cancelled")
            {
                TempData["Error"] = "Already cancelled.";
                return RedirectToAction("Index", "Bookings");
            }

            var fs = _config.GetSection("FareSettings");
            int fullRefundDays = fs.GetValue<int>("CancelFullRefundDays", 2);
            decimal oneDayDed =
                fs.GetValue<decimal>("CancelOneDayDeductionPercent", 15);
            decimal sameDayDed =
                fs.GetValue<decimal>("CancelSameDayDeductionPercent", 30);

            var daysLeft =
                (booking.Schedule.DepartureTime.Date - DateTime.Today).Days;

            decimal deductionPercent;
            if (daysLeft >= fullRefundDays) deductionPercent = 0;
            else if (daysLeft == 1) deductionPercent = oneDayDed;
            else deductionPercent = sameDayDed;

            decimal deductedAmount =
                Math.Round(booking.TotalAmount * deductionPercent / 100, 2);
            decimal refundAmount = booking.TotalAmount - deductedAmount;

            // Update booking
            booking.BookingStatus = "Cancelled";

            // Restore seats
            booking.Schedule.AvailableSeats +=
                booking.Passengers?.Count ?? 0;

            // Create cancellation record
            _context.Cancellations.Add(new Cancellation
            {
                BookingId = bookingId,
                CancellationDate = DateTime.Now,
                DeductionPercent = deductionPercent,
                DeductedAmount = deductedAmount,
                RefundAmount = refundAmount,
                RefundStatus = "Pending"
            });

            // Update payment status
            if (booking.Payment != null)
                booking.Payment.PaymentStatus = "Refunded";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Booking #{bookingId} cancelled. " +
                $"Refund: Rs.{refundAmount:F2} " +
                $"(Deducted: Rs.{deductedAmount:F2})";
            return RedirectToAction("Index", "Bookings");
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var cancellation = await _context.Cancellations
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Schedule)
                    .ThenInclude(s => s.Route)
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Schedule)
                    .ThenInclude(s => s.Bus)
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Passengers)
                .FirstOrDefaultAsync(c => c.CancellationId == id);
            if (cancellation == null) return NotFound();
            return View(cancellation);
        }

        [HttpPost]
        public async Task<IActionResult> MarkRefunded(int id)
        {
            var cancellation = await _context.Cancellations.FindAsync(id);
            if (cancellation == null) return NotFound();
            cancellation.RefundStatus = "Processed";
            await _context.SaveChangesAsync();
            TempData["Success"] = "Refund marked as processed.";
            return RedirectToAction(nameof(Index));
        }
    }
}