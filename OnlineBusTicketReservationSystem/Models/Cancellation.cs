using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBusTicketReservationSystem.Models
{
    public class Cancellation
    {
        [Key]
        public int CancellationId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; }

        [Display(Name = "Cancellation Date")]
        public DateTime CancellationDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Deduction %")]
        public decimal DeductionPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Deducted Amount")]
        public decimal DeductedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Refund Amount")]
        public decimal RefundAmount { get; set; }

        [MaxLength(20)]
        [Display(Name = "Refund Status")]
        public string RefundStatus { get; set; } = "Pending";

        [MaxLength(500)]
        [Display(Name = "Cancellation Reason")]
        public string? Reason { get; set; }

        [MaxLength(20)]
        [Display(Name = "Cancelled By")]
        public string CancelledBy { get; set; } = "Customer"; // "Customer" or "Admin"
    }
}