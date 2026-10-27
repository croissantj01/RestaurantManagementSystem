using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RestaurantManagementSystem.Models
{
    [Index(nameof(TableNumber), IsUnique = true)]
    public class RestaurantTable
    {
        public int RestaurantTableId { get; set; }

        [Required]
        [StringLength(20)]
        public string TableNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string DiningArea { get; set; }

        [Required]
        [Range(1, 20)]
        public int Capacity { get; set; }

        [Required]
        public string Status { get; set; } = "Available";
    }
}