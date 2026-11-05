using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;
using OnlineBusTicketReservationSystem.Models.ViewModels;

namespace OnlineBusTicketReservationSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : AdminBaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager) : base(context)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            decimal totalRevenue = await _context.Bookings
                .Where(b => b.BookingStatus == "Confirmed")
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            decimal totalRefunded = await _context.Cancellations
                .SumAsync(c => (decimal?)c.RefundAmount) ?? 0;

            // Monthly revenue — last 6 months
            var sixMonthsAgo = DateTime.Now.AddMonths(-5);
            var monthlyData = await _context.Bookings
                .Where(b => b.BookingStatus == "Confirmed" &&
                            b.BookingDate >= sixMonthsAgo)
                .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(b => b.TotalAmount),
                    Count   = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            // Fill missing months with 0
            var months      = new List<string>();
            var revenues    = new List<decimal>();
            var bookingCounts = new List<int>();
            for (int i = 5; i >= 0; i--)
            {
                var d = DateTime.Now.AddMonths(-i);
                months.Add(d.ToString("MMM yyyy"));
                var found = monthlyData.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                revenues.Add(found?.Revenue ?? 0);
                bookingCounts.Add(found?.Count ?? 0);
            }

            ViewBag.MonthLabels    = System.Text.Json.JsonSerializer.Serialize(months);
            ViewBag.MonthRevenues  = System.Text.Json.JsonSerializer.Serialize(revenues);
            ViewBag.MonthBookings  = System.Text.Json.JsonSerializer.Serialize(bookingCounts);

            // Unread messages badge

            ViewBag.PendingBookings   = await _context.Bookings.CountAsync(b => b.BookingStatus == "Pending");
            ViewBag.ConfirmedBookings = await _context.Bookings.CountAsync(b => b.BookingStatus == "Confirmed");
            ViewBag.CancelledBookings = await _context.Bookings.CountAsync(b => b.BookingStatus == "Cancelled");
            ViewBag.TotalRefunded     = totalRefunded;
            ViewBag.NetRevenue        = totalRevenue - totalRefunded;
            ViewBag.TotalCustomers    = (await _userManager.GetUsersInRoleAsync("Customer")).Count;

            var vm = new AdminDashboardViewModel
            {
                TotalBuses         = await _context.Buses.CountAsync(),
                ActiveBuses        = await _context.Buses.CountAsync(b => b.IsActive),
                TotalRoutes        = await _context.Routes.CountAsync(),
                TotalSchedules     = await _context.Schedules.CountAsync(),
                TotalEmployees     = (await _userManager.GetUsersInRoleAsync("Employee")).Count,
                TotalBookings      = await _context.Bookings.CountAsync(),
                TotalCancellations = await _context.Cancellations.CountAsync(),
                TotalRevenue       = totalRevenue,
                RecentBookings     = await _context.Bookings
                    .Include(b => b.BookedByUser)
                    .Include(b => b.Schedule).ThenInclude(s => s.Route)
                    .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(10)
                    .ToListAsync()
            };

            return View(vm);
        }
    }
}
