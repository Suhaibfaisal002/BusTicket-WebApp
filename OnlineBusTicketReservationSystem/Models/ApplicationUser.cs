using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OnlineBusTicketReservationSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string? FullName { get; set; }

        [Required]
        [MaxLength(10)]
        public string? Role { get; set; }  // "Admin" or "Employee"

        [MaxLength(6)]
        public string? EmployeeCode { get; set; }

        public int? Age { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        [MaxLength(150)]
        public string? Qualification { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ✅ 2FA Fields
        public bool TwoFactorEmailEnabled { get; set; } = false;
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }

        public ICollection<Booking> Bookings { get; set; }
    }
}