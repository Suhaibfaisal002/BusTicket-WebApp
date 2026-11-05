using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBusTicketReservationSystem.Models
{
    public class PromoCode
    {
        [Key]
        public int PromoCodeId { get; set; }

        [Required, MaxLength(20)]
        [Display(Name = "Code")]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required, MaxLength(10)]
        [Display(Name = "Discount Type")]
        public string DiscountType { get; set; } = "Percentage"; // "Percentage" or "Fixed"

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Discount Value")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Max Discount (Rs.)")]
        public decimal? MaxDiscount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Min Order Amount")]
        public decimal MinOrderAmount { get; set; } = 0;

        [Display(Name = "Usage Limit")]
        public int? UsageLimit { get; set; }

        [Display(Name = "Times Used")]
        public int TimesUsed { get; set; } = 0;

        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
