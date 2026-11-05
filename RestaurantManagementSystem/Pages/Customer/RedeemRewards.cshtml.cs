using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class RedeemRewardsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RedeemRewardsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string DisplayName { get; set; } = "";
        public int CustomerLoyaltyPoints { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null) return RedirectToPage("/Customer/Login");

            DisplayName = customer.FullName;
            CustomerLoyaltyPoints = customer.LoyaltyPoints;

            return Page();
        }

        public async Task<IActionResult> OnPostRedeemAsync(string rewardName, int pointsCost)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null) return RedirectToPage("/Customer/Login");

            if (customer.LoyaltyPoints < pointsCost)
            {
                ErrorMessage = $"You don't have enough points to redeem '{rewardName}'. You need {pointsCost} pts.";
                return RedirectToPage();
            }

            // Deduct points
            customer.LoyaltyPoints -= pointsCost;
            await _context.SaveChangesAsync();

            // Notify user of successful redemption
            await _context.AddNotificationAsync(customer.CustomerId,
                $"Successfully redeemed '{rewardName}' for {pointsCost} points! Check your profile or show this notification to staff.");

            StatusMessage = $"Success! You've redeemed: {rewardName}.";
            return RedirectToPage();
        }
    }
}