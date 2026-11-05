using Microsoft.AspNetCore.Mvc;

namespace OnlineBusTicketReservationSystem.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult Index(int statusCode)
        {
            if (statusCode == 404) return View("NotFound");
            if (statusCode == 403) return View("AccessDenied");
            return View("NotFound");
        }
    }
}
