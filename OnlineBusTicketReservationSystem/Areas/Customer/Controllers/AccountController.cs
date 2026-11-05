using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineBusTicketReservationSystem.Data;
using OnlineBusTicketReservationSystem.Models;
using OnlineBusTicketReservationSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace OnlineBusTicketReservationSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailService email)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _context       = context;
            _email         = email;
        }

        // ── REGISTER ──────────────────────────────────────────────────────
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Search", "Booking", new { area = "Customer" });
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string fullName, string email,
            string password, string confirmPassword,
            string? phone, string? gender, int? age)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View();
            }
            var user = new ApplicationUser
            {
                FullName    = fullName,
                UserName    = email,
                Email       = email,
                PhoneNumber = phone,
                Gender      = gender,
                Age         = age,
                Role        = "Customer",
                IsActive    = true,
                CreatedAt   = DateTime.Now
            };
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                await _signInManager.SignInAsync(user, isPersistent: false);
                try
                {
                    await _email.SendAsync(email, fullName,
                        "Welcome to SRC Travel! 🎊",
                        EmailTemplates.Welcome(fullName, email));
                }
                catch { }
                TempData["Success"] = $"Welcome {fullName}! Account created successfully.";
                return RedirectToAction("Search", "Booking", new { area = "Customer" });
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View();
        }

        // ── LOGIN ─────────────────────────────────────────────────────────
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Customer"))
                return RedirectToAction("Search", "Booking", new { area = "Customer" });
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.IsActive || !await _userManager.IsInRoleAsync(user, "Customer"))
            {
                ModelState.AddModelError("", "Invalid credentials or account not found.");
                return View();
            }

            // Password check karo pehle
            var passwordOk = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordOk)
            {
                // Lockout tracking
                await _userManager.AccessFailedAsync(user);
                if (await _userManager.IsLockedOutAsync(user))
                    ModelState.AddModelError("", "Account locked. Try again later.");
                else
                    ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }

            // ✅ 2FA enabled hai toh OTP bhejo
            if (user.TwoFactorEmailEnabled)
            {
                var otp    = new Random().Next(100000, 999999).ToString();
                user.OtpCode   = otp;
                user.OtpExpiry = DateTime.Now.AddMinutes(10);
                await _userManager.UpdateAsync(user);

                try
                {
                    await _email.SendAsync(user.Email!, user.FullName ?? "Customer",
                        "Your SRC Travel Login OTP",
                        EmailTemplates.OtpEmail(user.FullName ?? "Customer", otp));
                }
                catch { }

                // Email aur rememberMe temporarily store karo
                TempData["OtpEmail"]      = email;
                TempData["OtpRememberMe"] = rememberMe.ToString();
                return RedirectToAction(nameof(VerifyOtp));
            }

            // Normal login
            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
                TempData["Success"] = $"Welcome back, {user.FullName}!";
                return RedirectToAction("Search", "Booking", new { area = "Customer" });
            }
            if (result.IsLockedOut)
                ModelState.AddModelError("", "Account locked. Try again later.");
            else
                ModelState.AddModelError("", "Invalid email or password.");
            return View();
        }

        // ── OTP VERIFY ────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            if (TempData["OtpEmail"] == null)
                return RedirectToAction(nameof(Login));
            ViewBag.Email = TempData["OtpEmail"];
            TempData.Keep("OtpEmail");
            TempData.Keep("OtpRememberMe");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string email, string otp)
        {
            ViewBag.Email = email;
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return RedirectToAction(nameof(Login));

            if (user.OtpCode != otp || user.OtpExpiry < DateTime.Now)
            {
                ModelState.AddModelError("", user.OtpExpiry < DateTime.Now
                    ? "OTP expired. Please login again."
                    : "Invalid OTP. Please try again.");

                // Clear temp data
                TempData["OtpEmail"]      = email;
                TempData["OtpRememberMe"] = TempData["OtpRememberMe"];
                return View();
            }

            // OTP sahi — clear karo aur login
            user.OtpCode   = null;
            user.OtpExpiry = null;
            await _userManager.UpdateAsync(user);

            bool rememberMe = TempData["OtpRememberMe"]?.ToString() == "True";
            await _signInManager.SignInAsync(user, isPersistent: rememberMe);
            await _userManager.ResetAccessFailedCountAsync(user);

            TempData["Success"] = $"Welcome back, {user.FullName}!";
            return RedirectToAction("Search", "Booking", new { area = "Customer" });
        }

        // ── 2FA TOGGLE ────────────────────────────────────────────────────
        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle2FA()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            user.TwoFactorEmailEnabled = !user.TwoFactorEmailEnabled;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = user.TwoFactorEmailEnabled
                ? "Two-Factor Authentication enabled! OTP will be sent on login."
                : "Two-Factor Authentication disabled.";

            return RedirectToAction(nameof(Profile));
        }

        // ── LOGOUT ────────────────────────────────────────────────────────
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        // ── FORGOT PASSWORD ───────────────────────────────────────────────
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Please enter your email address.");
                return View();
            }
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && await _userManager.IsInRoleAsync(user, "Customer"))
            {
                var token     = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("ResetPassword", "Account",
                    new { area = "Customer", email = user.Email, token },
                    protocol: Request.Scheme)!;
                try
                {
                    await _email.SendAsync(email, user.FullName ?? "Customer",
                        "Password Reset Request — SRC Travel 🔐",
                        EmailTemplates.ForgotPassword(user.FullName ?? "Customer", resetLink));
                }
                catch
                {
                    TempData["Error"] = "Failed to send email. Please try again.";
                    return View();
                }
            }
            TempData["Success"] = "If this email is registered, a reset link has been sent.";
            return RedirectToAction(nameof(Login));
        }

        // ── RESET PASSWORD ────────────────────────────────────────────────
        [HttpGet]
        public IActionResult ResetPassword(string? email, string? token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid password reset link.";
                return RedirectToAction(nameof(Login));
            }
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            string email, string token, string newPassword, string confirmPassword)
        {
            ViewBag.Email = email;
            ViewBag.Token = token;
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View();
            }
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction(nameof(Login));
            }
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                TempData["Success"] = "Password reset successfully! Please login.";
                return RedirectToAction(nameof(Login));
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View();
        }

        // ── PROFILE ───────────────────────────────────────────────────────
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            var bookings = await _context.Bookings
                .Where(b => b.BookedByUserId == user.Id)
                .ToListAsync();
            ViewBag.TotalBookings  = bookings.Count;
            ViewBag.ConfirmedCount = bookings.Count(b => b.BookingStatus == "Confirmed");
            ViewBag.PendingCount   = bookings.Count(b => b.BookingStatus == "Pending");
            ViewBag.CancelledCount = bookings.Count(b => b.BookingStatus == "Cancelled");
            ViewBag.TotalSpent     = bookings.Where(b => b.BookingStatus == "Confirmed").Sum(b => b.TotalAmount);
            return View(user);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(string fullName, string? phone, string? gender, int? age)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            user.FullName    = fullName;
            user.PhoneNumber = phone;
            user.Gender      = gender;
            user.Age         = age;
            var result = await _userManager.UpdateAsync(user);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Profile updated successfully!"
                : "Failed to update profile.";
            return RedirectToAction(nameof(Profile));
        }

        // ── CHANGE PASSWORD ───────────────────────────────────────────────
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New passwords do not match.";
                return View();
            }
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction(nameof(Profile));
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View();
        }
    }
}
