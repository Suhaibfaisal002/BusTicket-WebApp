using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : AdminBaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public ReportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration config) : base(context)
        {
            _userManager = userManager;
            _config      = config;
        }

        // ── Booking Report (Index) ────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            toDate   ??= DateTime.Now;
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate   = toDate.Value.ToString("yyyy-MM-dd");

            var bookings = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.BookedByUser)
                .Include(b => b.Passengers)
                .Where(b => b.BookingDate.Date >= fromDate.Value.Date &&
                            b.BookingDate.Date <= toDate.Value.Date)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            ViewBag.TotalBookings   = bookings.Count;
            ViewBag.ConfirmedCount  = bookings.Count(b => b.BookingStatus == "Confirmed");
            ViewBag.PendingCount    = bookings.Count(b => b.BookingStatus == "Pending");
            ViewBag.CancelledCount  = bookings.Count(b => b.BookingStatus == "Cancelled");
            ViewBag.TotalRevenue    = bookings.Where(b => b.BookingStatus == "Confirmed").Sum(b => b.TotalAmount);
            ViewBag.TotalPassengers = bookings.Where(b => b.BookingStatus == "Confirmed").Sum(b => b.Passengers.Count);

            var popularRoutes = bookings
                .Where(b => b.BookingStatus == "Confirmed" && b.Schedule?.Route != null)
                .GroupBy(b => $"{b.Schedule!.Route!.SourceCity} => {b.Schedule.Route.DestinationCity}")
                .Select(g => new { Route = g.Key, Count = g.Count(), Revenue = g.Sum(b => b.TotalAmount) })
                .OrderByDescending(x => x.Count).Take(5).ToList();
            ViewBag.PopularRoutes = popularRoutes;

            var dailyData = bookings
                .Where(b => b.BookingStatus == "Confirmed")
                .GroupBy(b => b.BookingDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(b => b.TotalAmount) })
                .OrderBy(x => x.Date).ToList();
            ViewBag.ChartDates    = System.Text.Json.JsonSerializer.Serialize(dailyData.Select(d => d.Date.ToString("dd MMM")).ToList());
            ViewBag.ChartRevenues = System.Text.Json.JsonSerializer.Serialize(dailyData.Select(d => d.Revenue).ToList());

            return View(bookings);
        }

        // ── Revenue Report ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> RevenueReport(int? year)
        {
            year ??= DateTime.Now.Year;
            ViewBag.SelectedYear = year;
            ViewBag.Years = Enumerable.Range(DateTime.Now.Year - 3, 5).Reverse().ToList();

            decimal taxPct = _config.GetValue<decimal>("FareSettings:TaxPercent", 5m) / 100m;

            var bookings = await _context.Bookings
                .Include(b => b.Payment)
                .Where(b => b.BookingStatus == "Confirmed" && b.BookingDate.Year == year)
                .ToListAsync();

            var monthly = Enumerable.Range(1, 12).Select(m => {
                var mb    = bookings.Where(b => b.BookingDate.Month == m).ToList();
                var gross = mb.Sum(b => b.TotalAmount);
                var tax   = Math.Round(gross * taxPct, 2);
                return new
                {
                    Month = System.Globalization.CultureInfo.InvariantCulture
                                .DateTimeFormat.GetAbbreviatedMonthName(m),
                    Gross = gross,
                    Tax   = tax,
                    Net   = gross - tax,
                    Count = mb.Count
                };
            }).ToList();

            ViewBag.MonthlyData  = monthly;
            ViewBag.TotalGross   = monthly.Sum(m => m.Gross);
            ViewBag.TotalNet     = monthly.Sum(m => m.Net);
            ViewBag.TotalTax     = monthly.Sum(m => m.Tax);
            ViewBag.ChartLabels  = System.Text.Json.JsonSerializer.Serialize(monthly.Select(m => m.Month).ToList());
            ViewBag.ChartGross   = System.Text.Json.JsonSerializer.Serialize(monthly.Select(m => m.Gross).ToList());
            ViewBag.ChartNet     = System.Text.Json.JsonSerializer.Serialize(monthly.Select(m => m.Net).ToList());

            var cancellations = await _context.Cancellations
                .Where(c => c.Booking.BookingDate.Year == year)
                .ToListAsync();
            ViewBag.PendingRefunds   = cancellations.Count(c => c.RefundStatus == "Pending");
            ViewBag.ProcessedRefunds = cancellations.Count(c => c.RefundStatus == "Processed");
            ViewBag.TotalRefunds     = cancellations.Sum(c => c.RefundAmount);

            return View();
        }

        // ── Bus Utilization Report ────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> BusUtilization(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            toDate   ??= DateTime.Now;
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate   = toDate.Value.ToString("yyyy-MM-dd");

            var buses = await _context.Buses.ToListAsync();
            var schedules = await _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .Include(s => s.Bookings).ThenInclude(b => b.Passengers)
                .Where(s => s.DepartureTime.Date >= fromDate.Value.Date &&
                            s.DepartureTime.Date <= toDate.Value.Date)
                .ToListAsync();

            var utilization = buses.Select(bus => {
                var bs        = schedules.Where(s => s.BusId == bus.BusId).ToList();
                var allBkgs   = bs.SelectMany(s => s.Bookings).ToList();
                var confirmed = allBkgs.Where(b => b.BookingStatus == "Confirmed").ToList();
                var revenue   = confirmed.Sum(b => b.TotalAmount);
                var passengers = confirmed.Sum(b => b.Passengers.Count);
                int cap  = bus.TotalSeats > 0 ? bus.TotalSeats : 40;
                double util = bs.Count > 0 && cap > 0
                    ? Math.Round((double)passengers / (bs.Count * cap) * 100, 1) : 0;
                return new
                {
                    BusNumber      = bus.BusNumber,
                    BusCode        = bus.BusCode,
                    BusType        = bus.BusType,
                    OperatorName   = bus.OperatorName,
                    TotalSeats     = cap,
                    IsActive       = bus.IsActive,
                    TotalTrips     = bs.Count,
                    TotalBookings  = allBkgs.Count,
                    Confirmed      = confirmed.Count,
                    Passengers     = passengers,
                    Revenue        = revenue,
                    UtilizationPct = util
                };
            }).OrderByDescending(x => x.Revenue).ToList();

            var top5 = utilization.Take(5).ToList();
            ViewBag.ChartBuses    = System.Text.Json.JsonSerializer.Serialize(top5.Select(b => b.BusCode ?? b.BusNumber).ToList());
            ViewBag.ChartRevenues = System.Text.Json.JsonSerializer.Serialize(top5.Select(b => b.Revenue).ToList());
            ViewBag.ChartUtil     = System.Text.Json.JsonSerializer.Serialize(top5.Select(b => b.UtilizationPct).ToList());
            ViewBag.TotalBuses    = buses.Count;
            ViewBag.ActiveBuses   = buses.Count(b => b.IsActive);
            ViewBag.TotalRevenue  = utilization.Sum(b => b.Revenue);
            ViewBag.TopBus        = utilization.FirstOrDefault()?.BusNumber ?? "—";

            return View(utilization);
        }

        // ── Export Bookings to Excel ──────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ExportBookings(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            toDate   ??= DateTime.Now;

            var bookings = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.BookedByUser)
                .Include(b => b.Passengers)
                .Where(b => b.BookingDate.Date >= fromDate.Value.Date &&
                            b.BookingDate.Date <= toDate.Value.Date)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Bookings Report");

            var headers = new[] { "PNR", "Booking ID", "Customer Name", "Phone",
                                   "Route", "Bus", "Departure", "Passengers",
                                   "Total Amount (Rs.)", "Status", "Booking Date" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0f3460");
                cell.Style.Font.FontColor       = ClosedXML.Excel.XLColor.White;
                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            }

            int row = 2;
            foreach (var b in bookings)
            {
                ws.Cell(row, 1).Value  = b.PNR ?? $"#{b.BookingId:D4}";
                ws.Cell(row, 2).Value  = b.BookingId;
                ws.Cell(row, 3).Value  = b.CustomerName;
                ws.Cell(row, 4).Value  = b.CustomerPhone;
                ws.Cell(row, 5).Value  = $"{b.Schedule?.Route?.SourceCity} => {b.Schedule?.Route?.DestinationCity}";
                ws.Cell(row, 6).Value  = b.Schedule?.Bus?.BusNumber;
                ws.Cell(row, 7).Value  = b.Schedule?.DepartureTime.ToString("dd-MMM-yyyy HH:mm");
                ws.Cell(row, 8).Value  = b.Passengers?.Count ?? 0;
                ws.Cell(row, 9).Value  = (double)b.TotalAmount;
                ws.Cell(row, 10).Value = b.BookingStatus;
                ws.Cell(row, 11).Value = b.BookingDate.ToString("dd-MMM-yyyy HH:mm");

                var rowColor = b.BookingStatus == "Confirmed" ? "#f0fdf4"
                             : b.BookingStatus == "Cancelled"  ? "#fef2f2"
                             : "#fffbeb";
                ws.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml(rowColor);
                row++;
            }

            ws.Cell(row + 1, 9).Value = "Total Confirmed Revenue:";
            ws.Cell(row + 1, 9).Style.Font.Bold = true;
            ws.Cell(row + 1, 10).Value = (double)bookings
                .Where(b => b.BookingStatus == "Confirmed")
                .Sum(b => b.TotalAmount);
            ws.Cell(row + 1, 10).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"Bookings_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
