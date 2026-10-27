using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public int TableId { get; set; }

        // Date and time when the order is placed
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Date and time selected by customer for dine-in
        [Required]
        public DateTime DineInDateTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";

        // Navigation property
        public RestaurantTable? Table { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }
            = new List<OrderItem>();
    }
}