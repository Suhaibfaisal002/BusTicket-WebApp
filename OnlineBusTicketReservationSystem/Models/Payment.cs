using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBusTicketReservationSystem.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }

        [MaxLength(20)]
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; }

        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [MaxLength(20)]
        [Display(Name = "Payment Status")]
        public string? PaymentStatus { get; set; } = "Paid";
    }
}