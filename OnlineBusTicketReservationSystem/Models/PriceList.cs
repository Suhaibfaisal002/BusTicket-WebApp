using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBusTicketReservationSystem.Models
{
    public class PriceList
    {
        [Key]
        public int PriceId { get; set; }

        [Required]
        public int RouteId { get; set; }

        [ForeignKey("RouteId")]
        public Route? Route { get; set; }

        [Required]
        [MaxLength(20)]
        [Display(Name = "Bus Type")]
        public string BusType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Base Price")]
        public decimal BasePrice { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdatedAt { get; set; } = DateTime.Now;

        // string? because Identity UserId is string
        public string? UpdatedByUserId { get; set; }
    }
}