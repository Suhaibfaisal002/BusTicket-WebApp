using System.ComponentModel.DataAnnotations;

namespace OnlineBusTicketReservationSystem.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; }
    }

    public class CreateEmployeeViewModel
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string Password { get; set; }

        public int? Age { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        [MaxLength(150)]
        public string? Qualification { get; set; }
    }

    public class PassengerViewModel
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        public int Age { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        [MaxLength(10)]
        [Display(Name = "Seat No")]
        public string? SeatNumber { get; set; }
    }

    public class CreateBookingViewModel
    {
        [Required]
        [Display(Name = "Schedule")]
        public int ScheduleId { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }

        [Required]
        [MaxLength(15)]
        [Display(Name = "Customer Phone")]
        public string CustomerPhone { get; set; }

        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; }

        // ✅ Promo Code
        [MaxLength(20)]
        public string? PromoCode { get; set; }
        public decimal DiscountAmount { get; set; } = 0;

        public List<PassengerViewModel> Passengers { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        public int TotalBuses { get; set; }
        public int ActiveBuses { get; set; }
        public int TotalRoutes { get; set; }
        public int TotalSchedules { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalBookings { get; set; }
        public int TotalCancellations { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Booking> RecentBookings { get; set; } = new();
    }

    public class EmployeeDashboardViewModel
    {
        public int MyBookings { get; set; }
        public int TodayBookings { get; set; }
        public int MyCancellations { get; set; }
        public decimal MyRevenue { get; set; }
        public List<Booking> RecentBookings { get; set; } = new();
    }
}