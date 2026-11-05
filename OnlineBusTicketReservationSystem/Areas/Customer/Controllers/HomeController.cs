using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var routes = await _context.Routes
                .Include(r => r.RouteStops)
                .Take(6).ToListAsync();
            ViewBag.Routes = routes;

            var scheduleCount = await _context.Schedules
                .Where(s => s.IsActive).CountAsync();
            ViewBag.ScheduleCount = scheduleCount;

            return View();
        }

        public IActionResult About() => View();
        public IActionResult Services() => View();
        public IActionResult Cancellation() => View();
        public IActionResult Faqs() => View();
        public IActionResult PrivacyPolicy() => View();

        public async Task<IActionResult> PriceList()
        {
            var prices = await _context.PriceLists
                .Include(p => p.Route)
                .OrderBy(p => p.Route.SourceCity)
                .ToListAsync();
            return View(prices);
        }

        [HttpGet]
        public IActionResult Contact() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(
            string name, string email, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Name, Email and Message are required.";
                return View();
            }

            // DB mein save karo
            _context.ContactMessages.Add(new ContactMessage
            {
                Name    = name,
                Email   = email,
                Subject = subject,
                Message = message,
                SentAt  = DateTime.Now,
                IsRead  = false
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thank you! Your message has been received. We'll get back to you soon.";
            return RedirectToAction(nameof(Contact));
        }
    }
}
