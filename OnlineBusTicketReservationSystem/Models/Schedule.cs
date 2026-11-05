using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBusTicketReservationSystem.Models
{
    public class Schedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required]
        public int BusId { get; set; }
        [ForeignKey("BusId")]
        public Bus? Bus { get; set; }

        [Required]
        public int RouteId { get; set; }
        [ForeignKey("RouteId")]
        public Route? Route { get; set; }

        [Required]
        [Display(Name = "Departure Time")]
        public DateTime DepartureTime { get; set; }

        [Required]
        [Display(Name = "Arrival Time")]
        public DateTime ArrivalTime { get; set; }

        [Display(Name = "Available Seats")]
        public int AvailableSeats { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
