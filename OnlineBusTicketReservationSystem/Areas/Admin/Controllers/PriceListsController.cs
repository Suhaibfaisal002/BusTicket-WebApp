using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PriceListsController : AdminBaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public PriceListsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager) : base(context)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.PriceLists
                .Include(p => p.Route)
                .OrderBy(p => p.Route.SourceCity)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var price = await _context.PriceLists
                .Include(p => p.Route)
                .FirstOrDefaultAsync(p => p.PriceId == id);
            if (price == null) return NotFound();
            return View(price);
        }

        public IActionResult Create()
        {
            LoadDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("RouteId,BusType,BasePrice")] PriceList priceList)
        {
            if (ModelState.IsValid)
            {
                // Check duplicate
                bool exists = await _context.PriceLists.AnyAsync(p =>
                    p.RouteId == priceList.RouteId &&
                    p.BusType == priceList.BusType);
                if (exists)
                {
                    ModelState.AddModelError("",
                        "Price already exists for this route and bus type.");
                    LoadDropdowns();
                    return View(priceList);
                }

                priceList.LastUpdatedAt = DateTime.Now;
                priceList.UpdatedByUserId = _userManager.GetUserId(User);
                _context.Add(priceList);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Price created successfully.";
                return RedirectToAction(nameof(Index));
            }
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                TempData["Error"] = string.Join(" | ", errors);
                LoadDropdowns();
                return View(priceList);
            }
            LoadDropdowns();
            return View(priceList);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var price = await _context.PriceLists.FindAsync(id);
            if (price == null) return NotFound();
            LoadDropdowns(price.RouteId, price.BusType);
            return View(price);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("PriceId,RouteId,BusType,BasePrice")] PriceList priceList)
        {
            if (id != priceList.PriceId) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    priceList.LastUpdatedAt = DateTime.Now;
                    priceList.UpdatedByUserId = _userManager.GetUserId(User);
                    _context.Update(priceList);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Price updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.PriceLists
                        .AnyAsync(p => p.PriceId == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            LoadDropdowns(priceList.RouteId, priceList.BusType);
            return View(priceList);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var price = await _context.PriceLists
                .Include(p => p.Route)
                .FirstOrDefaultAsync(p => p.PriceId == id);
            if (price == null) return NotFound();
            return View(price);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var price = await _context.PriceLists.FindAsync(id);
            if (price != null) _context.PriceLists.Remove(price);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Price deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private void LoadDropdowns(
    int? routeId = null, string? busType = null)
        {
            var routes = _context.Routes
                .Select(r => new SelectListItem
                {
                    Value = r.RouteId.ToString(),
                    Text = r.SourceCity + " → " + r.DestinationCity,
                    Selected = r.RouteId == routeId
                }).ToList();

            ViewData["RouteId"] = routes;

            ViewData["BusTypes"] = new SelectList(
                new[] { "Express", "Luxury", "Volvo-NonAC", "Volvo-AC" },
                busType);
        }
    }
}