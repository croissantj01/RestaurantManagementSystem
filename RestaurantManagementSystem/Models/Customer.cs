using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [Phone]
        [StringLength(20)]
        public string Phone { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        // --- Membership & Loyalty Properties ---
        public bool IsMember { get; set; } = false;

        [StringLength(20)]
        public string MembershipTier { get; set; } = "Bronze"; // Bronze, Silver, Gold

        public int LoyaltyPoints { get; set; } = 0;

        public DateTime? MemberSince { get; set; }
    }
}