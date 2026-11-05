using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementSystem.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        // Who placed the order (null for orders made before this was tracked)
        public int? CustomerId { get; set; }

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

        public string PaymentStatus { get; set; } = "Unpaid";   // Unpaid / Paid
        public string? PaymentMethod { get; set; }
        public string? SpecialRequest { get; set; }

        // Navigation properties
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        public RestaurantTable? Table { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }
            = new List<OrderItem>();
    }
}