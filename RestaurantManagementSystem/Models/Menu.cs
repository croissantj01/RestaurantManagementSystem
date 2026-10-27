using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementSystem.Models
{
    public class Menu
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Menu name is required.")]
        [Display(Name = "Menu Name")]
        public string MenuName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required.")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 99999999.99,
            ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Display(Name = "Menu Image")]
        public string? ImagePath { get; set; }

        [Required]
        public string Category { get; set; }
    }
}
