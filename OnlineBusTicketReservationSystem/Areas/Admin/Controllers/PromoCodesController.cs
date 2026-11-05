using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PromoCodesController : AdminBaseController
    {
        public PromoCodesController(ApplicationDbContext context) : base(context) { }

        public async Task<IActionResult> Index()
        {
            var codes = await _context.PromoCodes
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(codes);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromoCode model)
        {
            // Uppercase karo
            model.Code = model.Code.ToUpper().Trim();

            // Duplicate check
            bool exists = await _context.PromoCodes
                .AnyAsync(p => p.Code == model.Code);
            if (exists)
            {
                ModelState.AddModelError("Code", "This promo code already exists.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                _context.PromoCodes.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Promo code '{model.Code}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var code = await _context.PromoCodes.FindAsync(id);
            if (code == null) return NotFound();
            code.IsActive = !code.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Promo code '{code.Code}' {(code.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var code = await _context.PromoCodes.FindAsync(id);
            if (code != null)
            {
                _context.PromoCodes.Remove(code);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Promo code '{code.Code}' deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ✅ AJAX — Customer ke liye promo code validate karo
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Validate(string code, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { valid = false, message = "Please enter a promo code." });

            var promo = await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Code == code.ToUpper().Trim() && p.IsActive);

            if (promo == null)
                return Json(new { valid = false, message = "Invalid or expired promo code." });

            if (promo.ExpiryDate.HasValue && promo.ExpiryDate < DateTime.Now)
                return Json(new { valid = false, message = "This promo code has expired." });

            if (promo.UsageLimit.HasValue && promo.TimesUsed >= promo.UsageLimit)
                return Json(new { valid = false, message = "This promo code has reached its usage limit." });

            if (amount < promo.MinOrderAmount)
                return Json(new { valid = false, message = $"Minimum order amount of Rs. {promo.MinOrderAmount:N0} required." });

            // Discount calculate karo
            decimal discount = promo.DiscountType == "Percentage"
                ? Math.Round(amount * promo.DiscountValue / 100, 2)
                : promo.DiscountValue;

            // Max discount cap
            if (promo.MaxDiscount.HasValue && discount > promo.MaxDiscount)
                discount = promo.MaxDiscount.Value;

            // Discount amount se zyada nahi ho sakta
            if (discount > amount) discount = amount;

            return Json(new
            {
                valid       = true,
                message     = $"✅ '{promo.Code}' applied! You save Rs. {discount:N0}",
                discount    = discount,
                finalAmount = amount - discount,
                description = promo.Description
            });
        }
    }
}
