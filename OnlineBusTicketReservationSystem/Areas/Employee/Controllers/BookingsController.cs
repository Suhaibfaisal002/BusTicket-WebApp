using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;
using OnlineBusTicketReservationSystem.Models.ViewModels;
using OnlineBusTicketReservationSystem.Services;

namespace OnlineBusTicketReservationSystem.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFareCalculatorService _fareCalc;
        private readonly IPnrService _pnr;

        public BookingsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IFareCalculatorService fareCalc,
            IPnrService pnr)
        {
            _context     = context;
            _userManager = userManager;
            _fareCalc    = fareCalc;
            _pnr         = pnr;
        }

        public async Task<IActionResult> Index(string? status, string? search)
        {
            ViewBag.Status = status;
            ViewBag.Search = search;

            var userId = _userManager.GetUserId(User);

            var query = _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Passengers)
                .Include(b => b.BookedByUser)
                .Where(b => b.BookedByUserId == userId)  // ✅ Sirf apni bookings
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.BookingStatus == status);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(b =>
                    b.CustomerName!.Contains(search) ||
                    b.CustomerPhone!.Contains(search));

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            ViewBag.TotalCount     = await _context.Bookings.CountAsync(b => b.BookedByUserId == userId);
            ViewBag.ConfirmedCount = await _context.Bookings.CountAsync(b => b.BookedByUserId == userId && b.BookingStatus == BookingStatus.Confirmed);
            ViewBag.PendingCount   = await _context.Bookings.CountAsync(b => b.BookedByUserId == userId && b.BookingStatus == BookingStatus.Pending);
            ViewBag.CancelledCount = await _context.Bookings.CountAsync(b => b.BookedByUserId == userId && b.BookingStatus == BookingStatus.Cancelled);

            return View(bookings);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var booking = await _context.Bookings
                .Include(b => b.BookedByUser)
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                    .ThenInclude(r => r.RouteStops)
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Passengers)
                .Include(b => b.Payment)
                .Include(b => b.Cancellation)
                .FirstOrDefaultAsync(b => b.BookingId == id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        public async Task<IActionResult> Create(int? scheduleId)
        {
            var vm = new CreateBookingViewModel();
            if (scheduleId.HasValue) vm.ScheduleId = scheduleId.Value;
            ViewData["Schedules"] = await GetScheduleSelectList(scheduleId);
            ViewData["PaymentMethods"] = new SelectList(
                new[] { "Cash", "Digital" });

            // Seat layout data
            if (scheduleId.HasValue)
            {
                var sch = await _context.Schedules
                    .Include(s => s.Bus)
                    .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId.Value);

                var bookedSeats = await _context.Bookings
                    .Where(b => b.ScheduleId == scheduleId.Value && b.BookingStatus != "Cancelled")
                    .SelectMany(b => b.Passengers)
                    .Select(p => p.SeatNumber)
                    .Where(s => s != null)
                    .ToListAsync();

                ViewBag.TotalSeats     = sch?.Bus?.TotalSeats ?? 0;
                ViewBag.AvailableSeats = sch?.AvailableSeats ?? 0;
                ViewBag.BookedSeats    = System.Text.Json.JsonSerializer.Serialize(bookedSeats);
                ViewBag.ScheduleIdSelected = scheduleId.Value;
            }
            else
            {
                ViewBag.TotalSeats     = 0;
                ViewBag.AvailableSeats = 0;
                ViewBag.BookedSeats    = "[]";
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingViewModel vm)
        {
            if (vm.Passengers == null || !vm.Passengers.Any())
            {
                ModelState.AddModelError("", "At least one passenger is required.");
                ViewData["Schedules"] = await GetScheduleSelectList(vm.ScheduleId);
                ViewData["PaymentMethods"] = new SelectList(new[] { "Cash", "Digital" }, vm.PaymentMethod);
                await LoadSeatViewBag(vm.ScheduleId);
                return View(vm);
            }

            var schedule = await _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .FirstOrDefaultAsync(s => s.ScheduleId == vm.ScheduleId);

            if (schedule == null)
            {
                ModelState.AddModelError("", "Invalid schedule.");
                ViewData["Schedules"] = await GetScheduleSelectList(vm.ScheduleId);
                ViewData["PaymentMethods"] = new SelectList(new[] { "Cash", "Digital" }, vm.PaymentMethod);
                await LoadSeatViewBag(vm.ScheduleId);
                return View(vm);
            }

            // Get base price
            var priceList = await _context.PriceLists
                .FirstOrDefaultAsync(p =>
                    p.RouteId == schedule.RouteId &&
                    p.BusType == schedule.Bus.BusType);

            decimal basePrice = priceList?.BasePrice ?? 0;

            // Calculate fares using shared service
            var passengers = new List<Passenger>();
            decimal totalAmount = 0;

            // Check if seats required
            var anyMissingSeat = vm.Passengers.Any(p => string.IsNullOrWhiteSpace(p.SeatNumber));
            if (anyMissingSeat)
            {
                ModelState.AddModelError("", "Please select a seat for each passenger from the seat layout.");
                ViewData["Schedules"] = await GetScheduleSelectList(vm.ScheduleId);
                ViewData["PaymentMethods"] = new SelectList(new[] { "Cash", "Digital" }, vm.PaymentMethod);
                await LoadSeatViewBag(vm.ScheduleId);
                return View(vm);
            }

            foreach (var p in vm.Passengers)
            {
                decimal fare = _fareCalc.Calculate(p.Age, basePrice);
                passengers.Add(new Passenger
                {
                    FullName   = p.FullName,
                    Age        = p.Age,
                    Gender     = p.Gender,
                    SeatNumber = p.SeatNumber,
                    FareAmount = fare
                });
                totalAmount += fare;
            }

            // Check seat availability
            if (schedule.AvailableSeats < passengers.Count)
            {
                ModelState.AddModelError("", $"Only {schedule.AvailableSeats} seats available.");
                ViewData["Schedules"] = await GetScheduleSelectList(vm.ScheduleId);
                ViewData["PaymentMethods"] = new SelectList(new[] { "Cash", "Digital" }, vm.PaymentMethod);
                await LoadSeatViewBag(vm.ScheduleId);
                return View(vm);
            }

            var userId = _userManager.GetUserId(User)!;

            // ✅ Promo code validate aur discount apply karo
            decimal finalDiscount = 0;
            if (!string.IsNullOrWhiteSpace(vm.PromoCode) && vm.DiscountAmount > 0)
            {
                var promo = await _context.PromoCodes
                    .FirstOrDefaultAsync(p => p.Code == vm.PromoCode.ToUpper() && p.IsActive);
                if (promo != null &&
                    (!promo.ExpiryDate.HasValue || promo.ExpiryDate >= DateTime.Now) &&
                    (!promo.UsageLimit.HasValue || promo.TimesUsed < promo.UsageLimit))
                {
                    finalDiscount = Math.Min(vm.DiscountAmount, totalAmount);
                    promo.TimesUsed++;
                }
            }

            var booking = new Booking
            {
                BookedByUserId = userId,
                ScheduleId     = vm.ScheduleId,
                CustomerName   = vm.CustomerName,
                CustomerPhone  = vm.CustomerPhone,
                TotalAmount    = totalAmount - finalDiscount,
                DiscountAmount = finalDiscount,
                PromoCode      = finalDiscount > 0 ? vm.PromoCode?.ToUpper() : null,
                BookingStatus  = BookingStatus.Confirmed,
                BookingDate    = DateTime.Now,
                Passengers     = passengers,
                PNR            = await _pnr.GenerateAsync()
            };

            _context.Bookings.Add(booking);
            schedule.AvailableSeats -= passengers.Count;
            await _context.SaveChangesAsync();

            // Create payment
            var payment = new Payment
            {
                BookingId     = booking.BookingId,
                AmountPaid    = totalAmount - finalDiscount,
                PaymentMethod = vm.PaymentMethod ?? "Cash",
                PaymentDate   = DateTime.Now,
                PaymentStatus = PaymentStatus.Paid
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Booking #{booking.BookingId} created! Total: Rs.{(totalAmount - finalDiscount):F2}";
            return RedirectToAction(nameof(PrintTicket),
                new { id = booking.BookingId });
        }

        public async Task<IActionResult> PrintTicket(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookedByUser)
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Passengers)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingId == id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        private async System.Threading.Tasks.Task LoadSeatViewBag(int scheduleId)
        {
            if (scheduleId <= 0)
            {
                ViewBag.TotalSeats = 0; ViewBag.AvailableSeats = 0;
                ViewBag.BookedSeats = "[]"; ViewBag.ScheduleIdSelected = null;
                return;
            }
            var sch = await _context.Schedules.Include(s => s.Bus)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
            var bookedSeats = await _context.Bookings
                .Where(b => b.ScheduleId == scheduleId && b.BookingStatus != BookingStatus.Cancelled)
                .SelectMany(b => b.Passengers)
                .Select(p => p.SeatNumber).Where(s => s != null).ToListAsync();
            ViewBag.TotalSeats         = sch?.Bus?.TotalSeats ?? 0;
            ViewBag.AvailableSeats     = sch?.AvailableSeats ?? 0;
            ViewBag.BookedSeats        = System.Text.Json.JsonSerializer.Serialize(bookedSeats);
            ViewBag.ScheduleIdSelected = scheduleId;
        }

        private async Task<SelectList> GetScheduleSelectList(
            int? selectedId = null)
        {
            var schedules = await _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .Where(s => s.IsActive && s.AvailableSeats > 0)
                .OrderBy(s => s.DepartureTime)
                .ToListAsync();

            var items = schedules.Select(s => new
            {
                s.ScheduleId,
                Display = $"{s.Route.SourceCity} → " +
                          $"{s.Route.DestinationCity} | " +
                          $"{s.Bus.BusNumber} ({s.Bus.BusType}) | " +
                          $"Dep: {s.DepartureTime:dd-MMM HH:mm} | " +
                          $"Seats: {s.AvailableSeats}"
            }).ToList();

            return new SelectList(items, "ScheduleId", "Display", selectedId);
        }
    }
}