using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CustomersController : AdminBaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context) : base(context)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            ViewBag.Search = search;
            ViewBag.Status = status;

            var customers = await _userManager.GetUsersInRoleAsync("Customer");

            if (!string.IsNullOrEmpty(search))
                customers = customers.Where(c =>
                    c.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.Email!.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (c.PhoneNumber ?? "").Contains(search))
                    .ToList();

            if (status == "active")
                customers = customers.Where(c => c.IsActive).ToList();
            else if (status == "blocked")
                customers = customers.Where(c => !c.IsActive).ToList();

            ViewBag.TotalCount   = customers.Count;
            ViewBag.ActiveCount  = customers.Count(c => c.IsActive);
            ViewBag.BlockedCount = customers.Count(c => !c.IsActive);

            return View(customers.OrderByDescending(c => c.CreatedAt).ToList());
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var bookings = await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Route)
                .Include(b => b.Schedule).ThenInclude(s => s.Bus)
                .Include(b => b.Passengers)
                .Where(b => b.BookedByUserId == id)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            ViewBag.Bookings        = bookings;
            ViewBag.TotalBookings   = bookings.Count;
            ViewBag.TotalSpent      = bookings.Where(b => b.BookingStatus == "Confirmed").Sum(b => b.TotalAmount);
            ViewBag.CancelledCount  = bookings.Count(b => b.BookingStatus == "Cancelled");

            return View(user);
        }


        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            bool hasBookings = await _context.Bookings.AnyAsync(b => b.BookedByUserId == id);
            if (hasBookings)
            {
                TempData["Error"] = "Cannot delete customer. They have existing bookings.";
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            bool hasBookings = await _context.Bookings.AnyAsync(b => b.BookedByUserId == id);
            if (hasBookings)
            {
                TempData["Error"] = "Cannot delete customer. They have existing bookings.";
                return RedirectToAction(nameof(Index));
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user != null) await _userManager.DeleteAsync(user);
            TempData["Success"] = "Customer deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            string action = user.IsActive ? "unblocked" : "blocked";
            TempData["Success"] = $"Customer '{user.FullName}' has been {action}.";

            return RedirectToAction(nameof(Index));
        }
    }
}
