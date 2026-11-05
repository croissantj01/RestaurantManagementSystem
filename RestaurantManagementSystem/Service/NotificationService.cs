using System.Threading.Tasks;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Services
{
    public static class NotificationService
    {
        public static async Task AddNotificationAsync(ApplicationDbContext context, int customerId, string message)
        {
            context.Notifications.Add(new Notification
            {
                CustomerId = customerId,
                Message = message,
                CreatedAt = DateTime.Now,
                IsRead = false
            });
            await context.SaveChangesAsync();
        }
    }
}