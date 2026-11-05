using System.ComponentModel.DataAnnotations;

namespace OnlineBusTicketReservationSystem.Models
{
    public class Bus
    {
        [Key]
        public int BusId { get; set; }

        [MaxLength(5)]
        public string? BusCode { get; set; }

        [Required]
        [MaxLength(20)]
        [Display(Name = "Bus Number")]
        public string? BusNumber { get; set; }

        [Required]
        [MaxLength(20)]
        [Display(Name = "Bus Type")]
        public string? BusType { get; set; }

        [Required]
        [Display(Name = "Total Seats")]
        public int TotalSeats { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Operator Name")]
        public string? OperatorName { get; set; }

        public bool IsActive { get; set; } = true;
    }
}