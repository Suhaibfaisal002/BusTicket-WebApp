using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBusTicketReservationSystem.Models
{
    public class RouteStop
    {
        [Key]
        public int StopId { get; set; }

        [Required]
        public int RouteId { get; set; }

        [ForeignKey("RouteId")]
        public Route Route { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Stop Name")]
        public string? StopName { get; set; }

        [Display(Name = "Stop Order")]
        public int StopOrder { get; set; }

        [Display(Name = "Distance from Source (KM)")]
        public double? DistanceFromSource { get; set; }
    }
}