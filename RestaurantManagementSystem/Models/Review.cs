using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        public int CustomerId { get; set; }

        // "Table Only" (Reservations table) or "Table + Food Order" (Orders table)
        [Required, MaxLength(30)]
        public string BookingType { get; set; } = "";

        // ReservationId or OrderId, depending on BookingType
        public int BookingId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}