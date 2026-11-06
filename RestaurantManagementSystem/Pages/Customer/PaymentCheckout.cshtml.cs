using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;
using Stripe;
using Stripe.Checkout;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class PaymentCheckoutModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentCheckoutModel(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public int OrderId { get; set; }

        public Order? Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            OrderId = orderId;
            Order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == OrderId && o.CustomerId == customerId.Value);

            if (Order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToPage("/Customer/OrderPortal");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostProcessPaymentAsync(int orderId)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var order = await _context.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId.Value);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToPage("/Customer/OrderPortal");
            }

            // Set your Secret Key from appsettings.json
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"] ?? "sk_test_51P...";

            var domain = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{domain}/Customer/PaymentSuccess?orderId={order.OrderId}";
            var cancelUrl = $"{domain}/Customer/PaymentCheckout/{order.OrderId}";

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(order.TotalAmount * 100),
                            Currency = "myr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Restaurant Order #ORD-{order.OrderId:D5}",
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }
    }
}