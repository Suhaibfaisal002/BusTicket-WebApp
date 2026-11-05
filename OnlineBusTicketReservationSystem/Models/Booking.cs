using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBusTicketReservationSystem.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        // ✅ PNR Number — unique booking reference
        [MaxLength(20)]
        [Display(Name = "PNR Number")]
        public string? PNR { get; set; }

        [Required]
        public string? BookedByUserId { get; set; }

        [ForeignKey("BookedByUserId")]
        public ApplicationUser BookedByUser { get; set; }

        [Required]
        public int ScheduleId { get; set; }

        [ForeignKey("ScheduleId")]
        public Schedule Schedule { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }

        [Required, MaxLength(15)]
        [Display(Name = "Customer Phone")]
        public string? CustomerPhone { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Required, MaxLength(20)]
        [Display(Name = "Booking Status")]
        public string? BookingStatus { get; set; } = "Pending";

        [Display(Name = "Booking Date")]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        // ✅ Promo Code
        [MaxLength(20)]
        public string? PromoCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Discount Amount")]
        public decimal DiscountAmount { get; set; } = 0;

        public ICollection<Passenger> Passengers { get; set; }
        public Cancellation Cancellation { get; set; }
        public Payment Payment { get; set; }
    }
}
