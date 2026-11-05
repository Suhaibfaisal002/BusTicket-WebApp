using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SchedulesController : AdminBaseController
    {

        public SchedulesController(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .OrderByDescending(s => s.DepartureTime)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var schedule = await _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);
            if (schedule == null) return NotFound();
            return View(schedule);
        }

        public IActionResult Create()
        {
            ViewData["BusId"] = new SelectList(
                _context.Buses.Where(b => b.IsActive),
                "BusId", "BusNumber");
            ViewData["RouteId"] = new SelectList(
                _context.Routes
                    .Select(r => new {
                        r.RouteId,
                        Display = r.SourceCity + " → " + r.DestinationCity
                    }),
                "RouteId", "Display");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("BusId,RouteId,DepartureTime,ArrivalTime,IsActive")]
            Schedule schedule)
        {
            if (ModelState.IsValid)
            {
                // ✅ Duplicate bus schedule check
                bool conflict = await _context.Schedules.AnyAsync(s =>
                    s.BusId == schedule.BusId &&
                    s.DepartureTime.Date == schedule.DepartureTime.Date);

                if (conflict)
                {
                    ModelState.AddModelError("",
                        "This bus is already scheduled on the same date. Please choose a different bus or date.");
                    LoadDropdowns(schedule.BusId, schedule.RouteId);
                    return View(schedule);
                }

                // Auto-set seats from bus
                var bus = await _context.Buses.FindAsync(schedule.BusId);
                schedule.AvailableSeats = bus?.TotalSeats ?? 0;

                _context.Add(schedule);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Schedule created successfully.";
                return RedirectToAction(nameof(Index));
            }
            LoadDropdowns(schedule.BusId, schedule.RouteId);
            return View(schedule);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();
            LoadDropdowns(schedule.BusId, schedule.RouteId);
            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("ScheduleId,BusId,RouteId,DepartureTime,ArrivalTime,AvailableSeats,IsActive")]
            Schedule schedule)
        {
            if (id != schedule.ScheduleId) return NotFound();
            if (ModelState.IsValid)
            {
                // ✅ Duplicate bus schedule check (exclude self)
                bool conflict = await _context.Schedules.AnyAsync(s =>
                    s.BusId == schedule.BusId &&
                    s.DepartureTime.Date == schedule.DepartureTime.Date &&
                    s.ScheduleId != schedule.ScheduleId);

                if (conflict)
                {
                    ModelState.AddModelError("",
                        "This bus is already scheduled on the same date. Please choose a different bus or date.");
                    LoadDropdowns(schedule.BusId, schedule.RouteId);
                    return View(schedule);
                }

                try
                {
                    _context.Update(schedule);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Schedule updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Schedules
                        .AnyAsync(s => s.ScheduleId == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            LoadDropdowns(schedule.BusId, schedule.RouteId);
            return View(schedule);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            bool hasBookings = await _context.Bookings
                .AnyAsync(b => b.ScheduleId == id);
            if (hasBookings)
            {
                TempData["Error"] =
                    "Cannot delete schedule. It has associated bookings.";
                return RedirectToAction(nameof(Index));
            }

            var schedule = await _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);
            if (schedule == null) return NotFound();
            return View(schedule);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool hasBookings = await _context.Bookings
                .AnyAsync(b => b.ScheduleId == id);
            if (hasBookings)
            {
                TempData["Error"] =
                    "Cannot delete schedule. It has associated bookings.";
                return RedirectToAction(nameof(Index));
            }
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null) _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Schedule deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();
            schedule.IsActive = !schedule.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] =
                $"Schedule status changed to {(schedule.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }

        private void LoadDropdowns(int busId, int routeId)
        {
            ViewData["BusId"] = new SelectList(
                _context.Buses.Where(b => b.IsActive),
                "BusId", "BusNumber", busId);
            ViewData["RouteId"] = new SelectList(
                _context.Routes
                    .Select(r => new {
                        r.RouteId,
                        Display = r.SourceCity + " → " + r.DestinationCity
                    }),
                "RouteId", "Display", routeId);
        }
    }
}