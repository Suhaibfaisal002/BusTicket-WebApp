using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;

namespace OnlineBusTicketReservationSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminBaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        public AdminBaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Har action se pehle automatically run hoga
        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            ViewBag.UnreadMessages = await _context.ContactMessages
                .CountAsync(m => !m.IsRead);
            ViewBag.PendingRefunds = await _context.Cancellations
                .CountAsync(c => c.RefundStatus == "Pending");

            await next();
        }
    }
}
