using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class PaymentSuccessModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PaymentSuccessModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int OrderId { get; set; }

        public Order? Order { get; set; }
        public int PointsEarned { get; set; }

        public async Task<IActionResult> OnGetAsync(int? orderId)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            int targetOrderId = orderId ?? OrderId;

            Order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == targetOrderId && o.CustomerId == customerId.Value);

            if (Order == null)
            {
                return RedirectToPage("/Customer/OrderPortal");
            }

            // Mark paid, assign table status, award points
            if (Order.PaymentStatus != "Paid")
            {
                Order.PaymentStatus = "Paid";
                Order.Status = "Reserved";

                if (Order.Table != null)
                {
                    Order.Table.Status = "Occupied";
                }

                var customer = await _context.Customers.FindAsync(customerId.Value);
                if (customer != null)
                {
                    PointsEarned = (int)Math.Floor(Order.TotalAmount);
                    customer.LoyaltyPoints += PointsEarned;

                    await _context.AddNotificationAsync(customer.CustomerId,
                        $"Payment successful for Order #{Order.OrderId}! You earned {PointsEarned} loyalty points.");
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                PointsEarned = (int)Math.Floor(Order.TotalAmount);
            }

            ViewData["CustomerName"] = HttpContext.Session.GetString("CustomerName") ?? "Guest";
            return Page();
        }
    }
}