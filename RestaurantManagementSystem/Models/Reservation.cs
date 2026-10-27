using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementSystem.Models
{
    public class Reservation
    {
        public int ReservationId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; } = null!;

        [Required]
        public int RestaurantTableId { get; set; }

        [ForeignKey("RestaurantTableId")]
        public RestaurantTable RestaurantTable { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        public DateTime ReservationDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan ReservationTime { get; set; }

        [Required]
        [Range(1, 20)]
        public int NumberOfGuests { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Reserved";
    }
}