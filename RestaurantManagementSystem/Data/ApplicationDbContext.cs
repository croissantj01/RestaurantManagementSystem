using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<RestaurantTable> RestaurantTables { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<CartItem> CartItems { get; set; } = default!;
        public DbSet<Announcement> Announcements { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        public DbSet<CartItem> CartItems { get; set; } = default!;

        public DbSet<Notification> Notifications { get; set; }

        // Helper method to add system notifications directly via DbContext
        public async Task AddNotificationAsync(int customerId, string message)
        {
            Notifications.Add(new Notification
            {
                CustomerId = customerId,
                Message = message,
                CreatedAt = DateTime.Now,
                IsRead = false
            });

            await SaveChangesAsync();
        }
            modelBuilder.Entity<Staff>().HasIndex(s => s.Email).IsUnique();
            modelBuilder.Entity<Staff>().HasIndex(s => s.Username).IsUnique();
            modelBuilder.Entity<Customer>().HasIndex(c => c.Email).IsUnique();
        }
    }
}
