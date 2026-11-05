using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;
using OnlineBusTicketReservationSystem.Models.ViewModels;

namespace OnlineBusTicketReservationSystem.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var today  = DateTime.Today;

            // Sirf Confirmed bookings ki revenue
            decimal myRevenue = await _context.Bookings
                .Where(b => b.BookedByUserId == userId &&
                            b.BookingStatus == "Confirmed")
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            var vm = new EmployeeDashboardViewModel
            {
                MyBookings      = await _context.Bookings
                    .CountAsync(b => b.BookedByUserId == userId),
                TodayBookings   = await _context.Bookings
                    .CountAsync(b => b.BookedByUserId == userId &&
                                    b.BookingDate.Date == today),
                MyCancellations = await _context.Bookings
                    .CountAsync(b => b.BookedByUserId == userId &&
                                    b.BookingStatus == "Cancelled"),
                MyRevenue       = myRevenue,
                RecentBookings  = await _context.Bookings
                    .Include(b => b.Schedule).ThenInclude(s => s.Route)
                    .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                    .Where(b => b.BookedByUserId == userId)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(10)
                    .ToListAsync()
            };

            ViewBag.PendingBookings   = await _context.Bookings
                .CountAsync(b => b.BookedByUserId == userId && b.BookingStatus == "Pending");
            ViewBag.ConfirmedBookings = await _context.Bookings
                .CountAsync(b => b.BookedByUserId == userId && b.BookingStatus == "Confirmed");

            return View(vm);
        }

        public async Task<IActionResult> Enquiry(
            string? sourceCity, string? destinationCity,
            DateTime? travelDate, string? busType)
        {
            ViewBag.SourceCity      = sourceCity;
            ViewBag.DestinationCity = destinationCity;
            ViewBag.TravelDate      = travelDate?.ToString("yyyy-MM-dd");
            ViewBag.BusType         = busType;

            var query = _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .Where(s => s.IsActive && s.AvailableSeats > 0);

            if (!string.IsNullOrEmpty(sourceCity))
                query = query.Where(s => s.Route.SourceCity.Contains(sourceCity));
            if (!string.IsNullOrEmpty(destinationCity))
                query = query.Where(s => s.Route.DestinationCity.Contains(destinationCity));
            if (travelDate.HasValue)
                query = query.Where(s => s.DepartureTime.Date == travelDate.Value.Date);
            if (!string.IsNullOrEmpty(busType))
                query = query.Where(s => s.Bus.BusType == busType);

            var results = await query.OrderBy(s => s.DepartureTime).ToListAsync();
            return View(results);
        }
    }
}
