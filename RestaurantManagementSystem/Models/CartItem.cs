using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementSystem.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int MenuId { get; set; }

        [Required]
        public string MenuName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string Size { get; set; } = "Small";
    }
}