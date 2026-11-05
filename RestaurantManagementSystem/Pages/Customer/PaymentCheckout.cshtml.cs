using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class PaymentCheckoutModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PaymentCheckoutModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int OrderId { get; set; }

        public Order? Order { get; set; }

        [BindProperty]
        public string PaymentMethod { get; set; } = "Credit Card";

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            OrderId = orderId;
            Order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Menu)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId.Value);

            if (Order == null)
            {
                return RedirectToPage("/Customer/Dashboard");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostProcessPaymentAsync(int orderId)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            OrderId = orderId;
            Order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Menu)
                .FirstOrDefaultAsync(o => o.OrderId == OrderId && o.CustomerId == customerId.Value);

            if (Order != null)
            {
                Order.PaymentStatus = "Paid";
                Order.PaymentMethod = PaymentMethod;
                Order.Status = "Completed";

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Payment successful for Order #ORD-{Order.OrderId:D5}!";
                return RedirectToPage("/Customer/Dashboard");
            }

            return Page();
        }
    }
}