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
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == OrderId && o.CustomerId == customerId.Value);

            if (Order != null && Order.Customer != null)
            {
                Order.PaymentStatus = "Paid";
                Order.PaymentMethod = PaymentMethod;
                Order.Status = "Completed";

                var customer = Order.Customer;
                int totalItemCount = Order.OrderItems.Sum(oi => oi.Quantity);

                // 1. Auto-trigger membership: 10+ items waives RM5 fee (FREE upgrade)
                if (!customer.IsMember && totalItemCount >= 10)
                {
                    customer.IsMember = true;
                    customer.MemberSince = DateTime.Now;
                    customer.MembershipTier = "Bronze";

                    await _context.AddNotificationAsync(customer.CustomerId,
                        "Congratulations! You unlocked FREE Bronze Membership for ordering 10+ items!");
                }

                // 2. Loyalty Points calculation based on tier multipliers
                if (customer.IsMember)
                {
                    // Gold: RM1.00 = 1 pt | Silver: RM1.50 = 1 pt | Bronze: RM2.00 = 1 pt
                    decimal multiplier = customer.MembershipTier switch
                    {
                        "Gold" => 1.00m,
                        "Silver" => 1.50m,
                        _ => 2.00m
                    };

                    int earnedPoints = (int)(Order.TotalAmount / multiplier);
                    customer.LoyaltyPoints += earnedPoints;

                    // 3. Tier Upgrade evaluation thresholds
                    if (customer.MembershipTier == "Bronze" && customer.LoyaltyPoints >= 1000)
                    {
                        customer.MembershipTier = "Silver";
                        await _context.AddNotificationAsync(customer.CustomerId,
                            "You've been upgraded to Silver Tier! Enjoy earning points faster (1 pt / RM1.50).");
                    }
                    else if (customer.MembershipTier == "Silver" && customer.LoyaltyPoints >= 2500)
                    {
                        customer.MembershipTier = "Gold";
                        await _context.AddNotificationAsync(customer.CustomerId,
                            "You've reached Gold Tier! You now earn 1 pt for every RM1.00 spent.");
                    }

                    await _context.AddNotificationAsync(customer.CustomerId,
                        $"You earned {earnedPoints} loyalty points from Order #ORD-{Order.OrderId:D5}!");
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Payment successful for Order #ORD-{Order.OrderId:D5}!";
                return RedirectToPage("/Customer/Dashboard");
            }

            return Page();
        }
    }
}