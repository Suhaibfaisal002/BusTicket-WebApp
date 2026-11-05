using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index", "Dashboard");
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string fullName, string? phone,
            string? gender, int? age, string? address, string? location, string? qualification)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index", "Dashboard");

            user.FullName     = fullName;
            user.PhoneNumber  = phone;
            user.Gender       = gender;
            user.Age          = age;
            user.Address      = address;
            user.Location     = location;
            user.Qualification = qualification;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                TempData["Success"] = "Profile updated successfully!";
            else
                TempData["Error"] = "Failed to update profile.";

            return RedirectToAction(nameof(Index));
        }
    }
}
