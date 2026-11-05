using System.ComponentModel.DataAnnotations;

namespace OnlineBusTicketReservationSystem.Models
{
    public class Route
    {
        [Key]
        public int RouteId { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Source City")]
        public string SourceCity { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Destination City")]
        public string DestinationCity { get; set; } = string.Empty;

        [Display(Name = "Distance (KM)")]
        public double? DistanceKm { get; set; }

        [Display(Name = "Estimated Duration")]
        public TimeSpan? EstimatedDuration { get; set; }

        public ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
    }
}