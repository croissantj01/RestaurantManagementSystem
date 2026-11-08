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

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must contain at least 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        // --- Membership & Loyalty Properties ---
        public bool IsMember { get; set; } = false;

        [StringLength(20)]
        public string MembershipTier { get; set; } = "Bronze"; // Bronze, Silver, Gold

        public int LoyaltyPoints { get; set; } = 0;

        public DateTime? MemberSince { get; set; }

        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }
        public bool IsEmailVerified { get; set; } = false;
    }
}