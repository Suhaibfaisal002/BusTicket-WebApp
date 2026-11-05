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
    public class UsersController : AdminBaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context) : base(context)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var employees = await _userManager
                .GetUsersInRoleAsync("Employee");
            return View(employees
                .OrderByDescending(e => e.CreatedAt).ToList());
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateEmployeeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Auto-generate unique 6-digit EmployeeCode
            var rng = new Random();
            string code;
            do
            {
                code = rng.Next(100000, 999999).ToString();
            }
            while (await _userManager.Users
                .AnyAsync(u => u.EmployeeCode == code));

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = "Employee",
                EmployeeCode = code,
                Age = model.Age,
                Gender = model.Gender,
                Address = model.Address,
                Location = model.Location,
                Qualification = model.Qualification,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager
                .CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Employee");
                TempData["Success"] =
                    $"Employee '{user.FullName}' created with code {user.EmployeeCode}.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id,
            ApplicationUser model)
        {
            if (id != model.Id) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Age = model.Age;
            user.Gender = model.Gender;
            user.Address = model.Address;
            user.Location = model.Location;
            user.Qualification = model.Qualification;
            user.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Employee updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(user);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            bool hasBookings = await _context.Bookings
                .AnyAsync(b => b.BookedByUserId == id);
            if (hasBookings)
            {
                TempData["Error"] =
                    "Cannot delete employee. They have associated bookings.";
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            bool hasBookings = await _context.Bookings
                .AnyAsync(b => b.BookedByUserId == id);
            if (hasBookings)
            {
                TempData["Error"] =
                    "Cannot delete employee. They have associated bookings.";
                return RedirectToAction(nameof(Index));
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
                await _userManager.DeleteAsync(user);

            TempData["Success"] = "Employee deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            TempData["Success"] =
                $"Employee status changed to {(user.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> EmployeePerformancePartial(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            toDate   ??= DateTime.Now;
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate   = toDate.Value.ToString("yyyy-MM-dd");

            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            var bookings  = await _context.Bookings
                .Where(b => b.BookingDate.Date >= fromDate.Value.Date &&
                            b.BookingDate.Date <= toDate.Value.Date)
                .ToListAsync();

            var performance = employees.Select(emp => {
                var eb        = bookings.Where(b => b.BookedByUserId == emp.Id).ToList();
                var confirmed = eb.Where(b => b.BookingStatus == "Confirmed").ToList();
                var revenue   = confirmed.Sum(b => b.TotalAmount);
                var rate      = eb.Count > 0
                    ? Math.Round((double)confirmed.Count / eb.Count * 100, 1) : 0.0;
                return new
                {
                    FullName      = emp.FullName ?? emp.Email,
                    EmpCode       = emp.EmployeeCode ?? "—",
                    TotalBookings = eb.Count,
                    Confirmed     = confirmed.Count,
                    Cancelled     = eb.Count(b => b.BookingStatus == "Cancelled"),
                    Pending       = eb.Count(b => b.BookingStatus == "Pending"),
                    Revenue       = revenue,
                    SuccessRate   = rate
                };
            }).OrderByDescending(x => x.Revenue).ToList();

            ViewBag.Performance      = performance;
            ViewBag.TotalEmpRevenue  = bookings
                .Where(b => b.BookingStatus == "Confirmed").Sum(b => b.TotalAmount);
            ViewBag.TopPerformer     = performance.FirstOrDefault()?.FullName ?? "—";
            return PartialView("_EmployeePerformancePartial");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(
            string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var token = await _userManager
                .GeneratePasswordResetTokenAsync(user);
            var result = await _userManager
                .ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
                TempData["Success"] = "Password reset successfully.";
            else
                TempData["Error"] = "Failed: " + string.Join(", ",
                    result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}