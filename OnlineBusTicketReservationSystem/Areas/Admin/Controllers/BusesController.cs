using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BusesController : AdminBaseController
    {

        public BusesController(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Buses
                .OrderByDescending(b => b.BusId).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var bus = await _context.Buses
                .FirstOrDefaultAsync(b => b.BusId == id);
            if (bus == null) return NotFound();
            return View(bus);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("BusNumber,BusType,TotalSeats,OperatorName,IsActive")] Bus bus)
        {
            if (ModelState.IsValid)
            {
                // Auto-generate unique 5-digit BusCode
                var rng = new Random();
                string code;
                do
                {
                    code = rng.Next(10000, 99999).ToString();
                }
                while (await _context.Buses
                    .AnyAsync(b => b.BusCode == code));

                bus.BusCode = code;
                _context.Add(bus);
                await _context.SaveChangesAsync();
                TempData["Success"] =
                    $"Bus '{bus.BusNumber}' created with code {bus.BusCode}.";
                return RedirectToAction(nameof(Index));
            }
            return View(bus);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var bus = await _context.Buses.FindAsync(id);
            if (bus == null) return NotFound();
            return View(bus);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("BusId,BusCode,BusNumber,BusType,TotalSeats,OperatorName,IsActive")] Bus bus)
        {
            if (id != bus.BusId) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    // When TotalSeats changes, update AvailableSeats on future schedules
                    var oldBus = await _context.Buses.AsNoTracking()
                        .FirstOrDefaultAsync(b => b.BusId == id);
                    int oldSeats = oldBus?.TotalSeats ?? bus.TotalSeats;
                    int seatDiff = bus.TotalSeats - oldSeats;

                    if (seatDiff != 0)
                    {
                        var futureSchedules = await _context.Schedules
                            .Where(s => s.BusId == id && s.DepartureTime > DateTime.Now)
                            .ToListAsync();
                        foreach (var sch in futureSchedules)
                        {
                            sch.AvailableSeats = Math.Max(0, sch.AvailableSeats + seatDiff);
                        }
                    }

                    _context.Update(bus);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Bus updated. Future schedules seat counts updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Buses.AnyAsync(b => b.BusId == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(bus);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            // Check dependent records
            bool hasSchedules = await _context.Schedules
                .AnyAsync(s => s.BusId == id);
            if (hasSchedules)
            {
                TempData["Error"] =
                    "Cannot delete bus. It has associated schedules.";
                return RedirectToAction(nameof(Index));
            }

            var bus = await _context.Buses
                .FirstOrDefaultAsync(b => b.BusId == id);
            if (bus == null) return NotFound();
            return View(bus);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool hasSchedules = await _context.Schedules
                .AnyAsync(s => s.BusId == id);
            if (hasSchedules)
            {
                TempData["Error"] =
                    "Cannot delete bus. It has associated schedules.";
                return RedirectToAction(nameof(Index));
            }
            var bus = await _context.Buses.FindAsync(id);
            if (bus != null) _context.Buses.Remove(bus);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Bus deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var bus = await _context.Buses.FindAsync(id);
            if (bus == null) return NotFound();
            bus.IsActive = !bus.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] =
                $"Bus status changed to {(bus.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}