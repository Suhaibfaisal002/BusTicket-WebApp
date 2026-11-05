using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MessagesController : AdminBaseController
    {
        public MessagesController(ApplicationDbContext context) : base(context) { }

        public async Task<IActionResult> Index()
        {
            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Booking)
                    .ThenInclude(b => b!.Schedule)
                    .ThenInclude(s => s!.Route)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            double avgRating = reviews.Count > 0
                ? Math.Round(reviews.Average(r => (double)r.Rating), 1) : 0.0;

            ViewBag.Reviews     = reviews;
            ViewBag.ReviewCount = reviews.Count(r => !r.IsRead);
            ViewBag.TotalReviews = reviews.Count;
            ViewBag.AvgRating   = avgRating;
            ViewBag.UnreadCount = messages.Count(m => !m.IsRead);
            return View(messages);
        }

        public async Task<IActionResult> Details(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();
            if (!msg.IsRead) { msg.IsRead = true; await _context.SaveChangesAsync(); }
            return View(msg);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg != null) { _context.ContactMessages.Remove(msg); await _context.SaveChangesAsync(); }
            TempData["Success"] = "Message deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null) { _context.Reviews.Remove(review); await _context.SaveChangesAsync(); }
            TempData["Success"] = "Review deleted.";
            return Redirect(Url.Action(nameof(Index))! + "#tabReviews");
        }
        [HttpPost]
        public async Task<IActionResult> MarkReviewsRead()
        {
            var unread = await _context.Reviews
                .Where(r => !r.IsRead)
                .ToListAsync();
            unread.ForEach(r => r.IsRead = true);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
