using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBusTicketReservationSystem.Models
{
    public class Passenger
    {
        [Key]
        public int PassengerId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [Required]
        public int Age { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        [MaxLength(10)]
        [Display(Name = "Seat Number")]
        public string? SeatNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Fare Amount")]
        public decimal FareAmount { get; set; }
    }
}