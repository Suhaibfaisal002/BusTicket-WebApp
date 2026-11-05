using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoutesController : AdminBaseController
    {

        public RoutesController(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Routes
                .Include(r => r.RouteStops)
                .OrderByDescending(r => r.RouteId)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var route = await _context.Routes
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync(r => r.RouteId == id);
            if (route == null) return NotFound();
            return View(route);
        }

        public IActionResult Create()
        {
            return View(new OnlineBusTicketReservationSystem.Models.Route
            {
                RouteStops = new List<RouteStop>()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            OnlineBusTicketReservationSystem.Models.Route route,
            List<string> StopNames,
            List<int> StopOrders,
            List<double?> StopDistances)
        {
            if (ModelState.IsValid)
            {
                route.RouteStops = new List<RouteStop>();
                for (int i = 0; i < StopNames.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(StopNames[i]))
                    {
                        route.RouteStops.Add(new RouteStop
                        {
                            StopName           = StopNames[i],
                            StopOrder          = StopOrders.Count > i ? StopOrders[i] : i + 1,
                            DistanceFromSource = StopDistances.Count > i ? StopDistances[i] : null
                        });
                    }
                }
                _context.Add(route);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Route '{route.SourceCity} → {route.DestinationCity}' created.";
                return RedirectToAction(nameof(Index));
            }
            return View(route);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var route = await _context.Routes
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync(r => r.RouteId == id);
            if (route == null) return NotFound();
            return View(route);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            OnlineBusTicketReservationSystem.Models.Route route,
            List<string> StopNames,
            List<int> StopOrders,
            List<double?> StopDistances)
        {
            if (id != route.RouteId) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    var oldStops = _context.RouteStops.Where(rs => rs.RouteId == id);
                    _context.RouteStops.RemoveRange(oldStops);

                    route.RouteStops = new List<RouteStop>();
                    if (StopNames != null)
                    {
                        for (int i = 0; i < StopNames.Count; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(StopNames[i]))
                            {
                                route.RouteStops.Add(new RouteStop
                                {
                                    StopName           = StopNames[i],
                                    StopOrder          = StopOrders?.Count > i ? StopOrders[i] : i + 1,
                                    DistanceFromSource = StopDistances?.Count > i ? StopDistances[i] : null
                                });
                            }
                        }
                    }
                    _context.Update(route);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Route updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Routes.AnyAsync(r => r.RouteId == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(route);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var route = await _context.Routes
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync(r => r.RouteId == id);
            if (route == null) return NotFound();

            // Schedules count dikhao warning ke liye
            int scheduleCount = await _context.Schedules.CountAsync(s => s.RouteId == id);
            ViewBag.ScheduleCount = scheduleCount;

            return View(route);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var route = await _context.Routes
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync(r => r.RouteId == id);

            if (route == null) return NotFound();

            // Pehle related bookings cancel karo aur seats restore karo
            var schedules = await _context.Schedules
                .Include(s => s.Bookings).ThenInclude(b => b.Passengers)
                .Include(s => s.Bookings).ThenInclude(b => b.Cancellation)
                .Where(s => s.RouteId == id)
                .ToListAsync();

            foreach (var schedule in schedules)
            {
                foreach (var booking in schedule.Bookings)
                {
                    if (booking.BookingStatus != "Cancelled")
                    {
                        // Add cancellation record if not exists
                        if (booking.Cancellation == null)
                        {
                            _context.Cancellations.Add(new Cancellation
                            {
                                BookingId        = booking.BookingId,
                                CancellationDate = DateTime.Now,
                                DeductionPercent = 0,
                                DeductedAmount   = 0,
                                RefundAmount     = booking.TotalAmount,
                                RefundStatus     = "Pending"
                            });
                        }
                        booking.BookingStatus = "Cancelled";
                    }
                }
                // Remove schedule
                _context.Schedules.Remove(schedule);
            }

            // Remove price lists for this route
            var priceLists = await _context.PriceLists
                .Where(p => p.RouteId == id).ToListAsync();
            _context.PriceLists.RemoveRange(priceLists);

            // Remove route (stops cascade delete)
            _context.Routes.Remove(route);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Route '{route.SourceCity} → {route.DestinationCity}' and all associated schedules deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
