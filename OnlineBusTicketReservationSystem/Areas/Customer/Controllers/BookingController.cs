using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;
using OnlineBusTicketReservationSystem.Services;

namespace OnlineBusTicketReservationSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly IEmailService _email;
        private readonly IFareCalculatorService _fareCalc;
        private readonly IPnrService _pnr;

        public BookingController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            IEmailService email,
            IFareCalculatorService fareCalc,
            IPnrService pnr)
        {
            _context     = context;
            _userManager = userManager;
            _config      = config;
            _email       = email;
            _fareCalc    = fareCalc;
            _pnr         = pnr;
        }

        // ✅ CUSTOMER DASHBOARD
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var bookings = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Passengers)
                .Include(b => b.Payment)
                .Where(b => b.BookedByUserId == user.Id)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var now = DateTime.Now;

            ViewBag.TotalBookings   = bookings.Count;
            ViewBag.ConfirmedCount  = bookings.Count(b => b.BookingStatus == "Confirmed");
            ViewBag.PendingCount    = bookings.Count(b => b.BookingStatus == "Pending");
            ViewBag.CancelledCount  = bookings.Count(b => b.BookingStatus == "Cancelled");
            ViewBag.TotalSpent      = bookings.Where(b => b.BookingStatus == "Confirmed").Sum(b => b.TotalAmount);
            ViewBag.TotalPassengers = bookings.Where(b => b.BookingStatus == "Confirmed").Sum(b => b.Passengers?.Count ?? 0);

            // Upcoming journeys
            ViewBag.UpcomingBookings = bookings
                .Where(b => b.BookingStatus == "Confirmed" && b.Schedule?.DepartureTime > now)
                .OrderBy(b => b.Schedule!.DepartureTime)
                .Take(3).ToList();

            // Recent bookings
            ViewBag.RecentBookings = bookings.Take(5).ToList();

            // Monthly spending chart — last 6 months
            var monthlySpend = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var d = now.AddMonths(-i);
                var amt = bookings
                    .Where(b => b.BookingStatus == "Confirmed" &&
                                b.BookingDate.Year == d.Year &&
                                b.BookingDate.Month == d.Month)
                    .Sum(b => b.TotalAmount);
                monthlySpend.Add(new { month = d.ToString("MMM"), amount = amt });
            }
            ViewBag.MonthlySpend = System.Text.Json.JsonSerializer.Serialize(monthlySpend);

            return View(user);
        }

        // SEARCH
        [AllowAnonymous]
        public async Task<IActionResult> Search(
            string? sourceCity, string? destinationCity,
            DateTime? travelDate)
        {
            ViewBag.SourceCity      = sourceCity;
            ViewBag.DestinationCity = destinationCity;
            ViewBag.TravelDate      = travelDate?.ToString("yyyy-MM-dd");

            var cities = await _context.Routes
                .Select(r => r.SourceCity)
                .Union(_context.Routes.Select(r => r.DestinationCity))
                .Distinct().OrderBy(c => c).ToListAsync();
            ViewBag.Cities = cities;

            var history = new List<string>();
            if (Request.Cookies.TryGetValue("SearchHistory", out var raw) && !string.IsNullOrEmpty(raw))
                history = raw.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!string.IsNullOrEmpty(sourceCity) && !string.IsNullOrEmpty(destinationCity))
            {
                var entry = $"{sourceCity}➜{destinationCity}";
                history.Remove(entry);
                history.Insert(0, entry);
                if (history.Count > 5) history = history.Take(5).ToList();
                Response.Cookies.Append("SearchHistory", string.Join("|", history),
                    new CookieOptions { Expires = DateTimeOffset.Now.AddDays(30) });
            }
            ViewBag.SearchHistory = history;

            if (string.IsNullOrEmpty(sourceCity) || string.IsNullOrEmpty(destinationCity))
                return View(new List<Schedule>());

            var date      = travelDate ?? DateTime.Today;
            var schedules = await _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                    .ThenInclude(r => r.RouteStops)
                .Where(s => s.IsActive &&
                    s.Route.SourceCity.ToLower() == sourceCity.ToLower() &&
                    s.Route.DestinationCity.ToLower() == destinationCity.ToLower() &&
                    s.DepartureTime.Date == date.Date &&
                    s.AvailableSeats > 0)
                .ToListAsync();

            return View(schedules);
        }

        // ✅ AJAX SEARCH — returns JSON for live search
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> SearchAjax(
            string? sourceCity, string? destinationCity, DateTime? travelDate)
        {
            if (string.IsNullOrEmpty(sourceCity) || string.IsNullOrEmpty(destinationCity))
                return Json(new { success = false, message = "Please select source and destination." });

            var date      = travelDate ?? DateTime.Today;
            var schedules = await _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .Where(s => s.IsActive &&
                    s.Route.SourceCity.ToLower() == sourceCity.ToLower() &&
                    s.Route.DestinationCity.ToLower() == destinationCity.ToLower() &&
                    s.DepartureTime.Date == date.Date &&
                    s.AvailableSeats > 0)
                .OrderBy(s => s.DepartureTime)
                .ToListAsync();

            var result = schedules.Select(s => new
            {
                scheduleId    = s.ScheduleId,
                busCode       = s.Bus?.BusCode,
                busType       = s.Bus?.BusType,
                operatorName  = s.Bus?.OperatorName,
                sourceCity    = s.Route?.SourceCity,
                destCity      = s.Route?.DestinationCity,
                departure     = s.DepartureTime.ToString("HH:mm"),
                arrival       = s.ArrivalTime.ToString("HH:mm"),
                totalSeats    = s.Bus?.TotalSeats ?? 40,
                availSeats    = s.AvailableSeats,
            });

            return Json(new { success = true, count = schedules.Count, schedules = result });
        }

        // BOOK GET
        [HttpGet]
        public async Task<IActionResult> Book(int scheduleId)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
            if (schedule == null) return NotFound();

            var fare = await _context.PriceLists
                .FirstOrDefaultAsync(p =>
                    p.RouteId == schedule.RouteId &&
                    p.BusType == schedule.Bus!.BusType);

            var bookedSeats = await _context.Bookings
                .Where(b => b.ScheduleId == scheduleId && b.BookingStatus != "Cancelled")
                .SelectMany(b => b.Passengers)
                .Select(p => p.SeatNumber)
                .Where(s => s != null)
                .ToListAsync();

            ViewBag.BasePrice      = fare?.BasePrice ?? 0;
            ViewBag.FreeAge        = _config.GetValue<int>("FareSettings:ChildAgeFree");
            ViewBag.HalfAge        = _config.GetValue<int>("FareSettings:TeenAgeHalf");
            ViewBag.SeniorAge      = _config.GetValue<int>("FareSettings:SeniorAgeDiscount");
            ViewBag.BookedSeats    = System.Text.Json.JsonSerializer.Serialize(bookedSeats);
            ViewBag.TotalSeats     = schedule.Bus?.TotalSeats ?? 40;
            ViewBag.AvailableSeats = schedule.AvailableSeats;
            return View(schedule);
        }

        // BOOK POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int scheduleId,
            string customerName, string customerPhone,
            List<string> passengerNames,
            List<int> passengerAges,
            List<string> passengerGenders,
            List<string>? passengerSeats = null,
            string? promoCode = null,
            decimal discountAmount = 0)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
            if (schedule == null) return NotFound();

            if (passengerNames == null || passengerNames.Count == 0)
            {
                TempData["Error"] = "Please add at least one passenger.";
                return RedirectToAction(nameof(Book), new { scheduleId });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var fare = await _context.PriceLists
                .FirstOrDefaultAsync(p =>
                    p.RouteId == schedule.RouteId &&
                    p.BusType == schedule.Bus!.BusType);

            decimal basePrice = fare?.BasePrice ?? 0;

            var passengers  = new List<Passenger>();
            decimal totalAmount = 0;

            for (int i = 0; i < passengerNames.Count; i++)
            {
                int age    = passengerAges.Count > i ? passengerAges[i] : 25;
                decimal pFare = _fareCalc.Calculate(age, basePrice);

                totalAmount += pFare;
                passengers.Add(new Passenger
                {
                    FullName   = passengerNames[i],
                    Age        = age,
                    Gender     = passengerGenders.Count > i ? passengerGenders[i] : "Male",
                    FareAmount = pFare,
                    SeatNumber = passengerSeats != null && passengerSeats.Count > i ? passengerSeats[i] : null
                });
            }

            // Seat required validation
            var missingSeat = passengers.Any(p => string.IsNullOrWhiteSpace(p.SeatNumber));
            if (missingSeat)
            {
                TempData["Error"] = "Please select a seat for each passenger from the seat layout.";
                return RedirectToAction(nameof(Book), new { scheduleId });
            }

            // ✅ Race condition fix — transaction use karo
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Re-check seats inside transaction
                var freshSchedule = await _context.Schedules
                    .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
                if (freshSchedule == null || freshSchedule.AvailableSeats < passengers.Count)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = $"Sorry, only {freshSchedule?.AvailableSeats ?? 0} seats available now.";
                    return RedirectToAction(nameof(Book), new { scheduleId });
                }

                // ✅ Promo code validate aur discount apply karo
                decimal finalDiscount = 0;
                if (!string.IsNullOrWhiteSpace(promoCode) && discountAmount > 0)
                {
                    var promo = await _context.PromoCodes
                        .FirstOrDefaultAsync(p => p.Code == promoCode.ToUpper() && p.IsActive);
                    if (promo != null &&
                        (!promo.ExpiryDate.HasValue || promo.ExpiryDate >= DateTime.Now) &&
                        (!promo.UsageLimit.HasValue || promo.TimesUsed < promo.UsageLimit))
                    {
                        finalDiscount = Math.Min(discountAmount, totalAmount);
                        promo.TimesUsed++;
                    }
                }

                var booking = new Booking
                {
                    BookingDate    = DateTime.Now,
                    CustomerName   = customerName,
                    CustomerPhone  = customerPhone,
                    TotalAmount    = totalAmount - finalDiscount,
                    DiscountAmount = finalDiscount,
                    PromoCode      = finalDiscount > 0 ? promoCode?.ToUpper() : null,
                    BookingStatus  = BookingStatus.Pending,
                    BookedByUserId = user.Id,
                    ScheduleId     = scheduleId,
                    Passengers     = passengers,
                    PNR            = await _pnr.GenerateAsync(),
                    Payment = new Payment
                    {
                        AmountPaid    = totalAmount - finalDiscount,
                        PaymentMethod = "Online",
                        PaymentDate   = DateTime.Now,
                        PaymentStatus = PaymentStatus.Pending
                    }
                };

                _context.Bookings.Add(booking);
                freshSchedule.AvailableSeats -= passengers.Count;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Email — Booking Pending
                try
                {
                    var route     = $"{schedule.Route?.SourceCity} → {schedule.Route?.DestinationCity}";
                    var departure = schedule.DepartureTime.ToString("dd MMM yyyy, HH:mm");
                    await _email.SendAsync(
                        user.Email!,
                        user.FullName ?? "Customer",
                        "Booking Received — SRC Travel",
                        EmailTemplates.BookingPending(
                            customerName, booking.BookingId, route, departure, totalAmount));
                }
                catch { }

                TempData["PendingBookingId"] = booking.BookingId;
                return RedirectToAction(nameof(Payment), new { id = booking.BookingId });
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Booking failed due to a system error. Please try again.";
                return RedirectToAction(nameof(Book), new { scheduleId });
            }
        }

        // PAYMENT GATEWAY
        [HttpGet]
        public async Task<IActionResult> Payment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Customer" });

            var booking = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.BookedByUserId == user.Id);
            if (booking == null) return NotFound();

            // ✅ YEH 4 LINES ADD KARO:
            ViewBag.BookingId = booking.BookingId;
            ViewBag.Amount = booking.TotalAmount;
            ViewBag.Route = $"{booking.Schedule?.Route?.SourceCity} → {booking.Schedule?.Route?.DestinationCity}";
            ViewBag.Departure = booking.Schedule?.DepartureTime.ToString("dd MMM yyyy, HH:mm");

            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int id, string paymentMethod)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Customer" });

            var booking = await _context.Bookings
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.BookedByUserId == user.Id);
            if (booking == null) return NotFound();

            if (booking.Payment != null)
            {
                booking.Payment.PaymentMethod = paymentMethod;
                booking.Payment.PaymentStatus = "Paid";
                booking.Payment.PaymentDate = DateTime.Now;
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Payment successful! Your booking is being reviewed.";
            return RedirectToAction(nameof(BookingConfirmed), new { id });
        }

        // BOOKING CONFIRMED PAGE
        public async Task<IActionResult> BookingConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Passengers)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == id &&
                    b.BookedByUserId == user!.Id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        // MY BOOKINGS
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            var bookings = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Passengers)
                .Where(b => b.BookedByUserId == user!.Id)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            // Which bookings already have a review
            var reviewedIds = await _context.Reviews
                .Where(r => r.UserId == user!.Id)
                .Select(r => r.BookingId)
                .ToListAsync();
            ViewBag.ReviewedBookingIds = new System.Collections.Generic.HashSet<int>(reviewedIds);

            return View(bookings);
        }

        // PAYMENT HISTORY
        public async Task<IActionResult> PaymentHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            var bookings = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Payment)
                .Include(b => b.Cancellation)
                .Where(b => b.BookedByUserId == user!.Id && b.Payment != null)
                .OrderByDescending(b => b.Payment!.PaymentDate)
                .ToListAsync();

            ViewBag.TotalPaid     = bookings.Where(b => b.BookingStatus == "Confirmed").Sum(b => b.Payment!.AmountPaid);
            ViewBag.TotalRefunded = bookings.Where(b => b.Cancellation != null).Sum(b => b.Cancellation!.RefundAmount);
            ViewBag.TotalBookings = bookings.Count;

            return View(bookings);
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Passengers)
                .Include(b => b.Payment)
                .Include(b => b.Cancellation)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == id &&
                    b.BookedByUserId == user!.Id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        // PRINT TICKET
        public async Task<IActionResult> PrintTicket(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Passengers)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == id &&
                    b.BookedByUserId == user!.Id &&
                    b.BookingStatus == "Confirmed");
            if (booking == null)
            {
                TempData["Error"] = "Ticket can only be printed for confirmed bookings.";
                return RedirectToAction(nameof(MyBookings));
            }
            return View(booking);
        }

        // CANCEL GET
        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Passengers)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == id &&
                    b.BookedByUserId == user!.Id &&
                    (b.BookingStatus == "Confirmed" || b.BookingStatus == "Pending"));
            if (booking == null) return NotFound();

            int fullRefundDays = _config.GetValue<int>("FareSettings:CancelFullRefundDays");
            int oneDayPct      = _config.GetValue<int>("FareSettings:CancelOneDayDeductionPercent");
            int sameDayPct     = _config.GetValue<int>("FareSettings:CancelSameDayDeductionPercent");

            var daysUntil = (booking.Schedule!.DepartureTime.Date - DateTime.Today).Days;

            decimal deductPct = 0;
            if (booking.BookingStatus == "Confirmed")
            {
                if (daysUntil >= fullRefundDays)   deductPct = 0;
                else if (daysUntil == 1)           deductPct = oneDayPct;
                else                               deductPct = sameDayPct;
            }

            decimal deductAmount = booking.TotalAmount * (deductPct / 100m);
            decimal refundAmount = booking.TotalAmount - deductAmount;

            ViewBag.DaysUntil    = daysUntil;
            ViewBag.DeductPct    = deductPct;
            ViewBag.DeductAmount = deductAmount;
            ViewBag.RefundAmount = refundAmount;

            // ✅ Yahan reasons properly set ho rahe hain
            ViewBag.CancelReasons = new List<string>
            {
                "Change of plans",
                "Found a better option",
                "Emergency / health issue",
                "Incorrect booking details",
                "Travel date changed",
                "Other"
            };

            return View(booking);
        }

        // CANCEL POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id, string? reason)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Passengers)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == id &&
                    b.BookedByUserId == user!.Id &&
                    (b.BookingStatus == "Confirmed" || b.BookingStatus == "Pending"));
            if (booking == null) return NotFound();

            int fullRefundDays = _config.GetValue<int>("FareSettings:CancelFullRefundDays");
            int oneDayPct      = _config.GetValue<int>("FareSettings:CancelOneDayDeductionPercent");
            int sameDayPct     = _config.GetValue<int>("FareSettings:CancelSameDayDeductionPercent");

            var daysUntil = (booking.Schedule!.DepartureTime.Date - DateTime.Today).Days;

            decimal deductPct = 0;
            if (booking.BookingStatus == "Confirmed")
            {
                if (daysUntil >= fullRefundDays)   deductPct = 0;
                else if (daysUntil == 1)           deductPct = oneDayPct;
                else                               deductPct = sameDayPct;
            }

            decimal deductAmount = booking.TotalAmount * (deductPct / 100m);
            decimal refundAmount = booking.TotalAmount - deductAmount;

            // ✅ Reason aur CancelledBy save ho raha hai
            _context.Cancellations.Add(new Cancellation
            {
                BookingId        = id,
                CancellationDate = DateTime.Now,
                DeductionPercent = deductPct,
                DeductedAmount   = deductAmount,
                RefundAmount     = refundAmount,
                RefundStatus     = "Pending",
                Reason           = reason,
                CancelledBy      = "Customer"
            });

            booking.BookingStatus = "Cancelled";
            booking.Schedule!.AvailableSeats += booking.Passengers?.Count ?? 0;
            await _context.SaveChangesAsync();

            // ✅ Email — Cancellation notification
            try
            {
                var route = $"{booking.Schedule?.Route?.SourceCity} → {booking.Schedule?.Route?.DestinationCity}";
                await _email.SendAsync(
                    user!.Email!,
                    user.FullName ?? "Customer",
                    "Booking Cancelled — SRC Travel",
                    EmailTemplates.BookingCancelled(
                        booking.CustomerName ?? "Customer",
                        booking.BookingId,
                        route,
                        refundAmount,
                        "Pending"));
            }
            catch { }

            TempData["Success"] = $"Booking cancelled. Refund of Rs. {refundAmount:N0} will be processed within 3-5 business days.";
            return RedirectToAction(nameof(MyBookings));
        }

        // ── REVIEW ───────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> LeaveReview(int bookingId)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId &&
                    b.BookedByUserId == user!.Id &&
                    b.BookingStatus == "Confirmed");
            if (booking == null) return NotFound();

            // Check already reviewed
            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.BookingId == bookingId && r.UserId == user!.Id);
            if (alreadyReviewed)
            {
                TempData["Error"] = "You have already submitted a review for this booking.";
                return RedirectToAction(nameof(MyBookings));
            }
            return View(booking);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int bookingId, int rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.BookingId == bookingId && r.UserId == user!.Id);
            if (alreadyReviewed)
            {
                TempData["Error"] = "Already reviewed.";
                return RedirectToAction(nameof(MyBookings));
            }
            _context.Reviews.Add(new OnlineBusTicketReservationSystem.Models.Review
            {
                BookingId = bookingId,
                UserId    = user!.Id,
                Rating    = Math.Clamp(rating, 1, 5),
                Comment   = comment?.Trim(),
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thank you for your review!";
            return RedirectToAction(nameof(MyBookings));
        }
    }
}
