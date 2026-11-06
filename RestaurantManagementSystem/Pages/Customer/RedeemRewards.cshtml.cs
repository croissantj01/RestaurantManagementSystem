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

        public string DisplayName { get; set; } = "Guest";
        public int CustomerLoyaltyPoints { get; set; }
        public List<RedeemedVoucher> RedeemedCodes { get; set; } = new List<RedeemedVoucher>();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            DisplayName = customer.FullName;
            ViewData["CustomerName"] = DisplayName;
            CustomerLoyaltyPoints = customer.LoyaltyPoints;

            // Fetch history of redeemed codes for this customer
            RedeemedCodes = await _context.RedeemedVouchers
                .Where(v => v.CustomerId == customerId.Value)
                .OrderByDescending(v => v.RedeemedDate)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostRedeemAsync(string rewardName, int pointsCost)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null) return RedirectToPage("/Customer/Login");

            string cleanRewardName = rewardName?.Trim() ?? "";

            // 1. Check if the customer has an UNUSED voucher for this specific reward
            bool hasActiveUnusedVoucher = await _context.RedeemedVouchers
                .AnyAsync(v => v.CustomerId == customerId.Value
                            && v.RewardName.ToLower() == cleanRewardName.ToLower()
                            && !v.IsUsed);

            if (hasActiveUnusedVoucher)
            {
                ErrorMessage = $"You already have an active unused promo code for '{cleanRewardName}'. Please use it before claiming another one.";
                return RedirectToPage();
            }

            // 2. Check if customer has enough loyalty points
            if (customer.LoyaltyPoints < pointsCost)
            {
                ErrorMessage = $"You don't have enough points to redeem '{cleanRewardName}'. You need {pointsCost} pts.";
                return RedirectToPage();
            }

            // Deduct points
            customer.LoyaltyPoints -= pointsCost;

            // Generate promo code
            string prefix = cleanRewardName.Contains("Drink", StringComparison.OrdinalIgnoreCase) ? "DRINK" :
                            cleanRewardName.Contains("Dessert", StringComparison.OrdinalIgnoreCase) ? "DESSERT" : "VOUCHER10";
            string generatedCode = $"{prefix}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";

            var newVoucher = new RedeemedVoucher
            {
                CustomerId = customer.CustomerId,
                RewardName = cleanRewardName,
                PromoCode = generatedCode,
                RedeemedDate = DateTime.Now,
                IsUsed = false
            };

            _context.RedeemedVouchers.Add(newVoucher);
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            await _context.AddNotificationAsync(customer.CustomerId,
                $"Successfully redeemed '{cleanRewardName}'! Your Promo Code is: {generatedCode}");

            StatusMessage = $"Success! Your promo code for '{cleanRewardName}' is: {generatedCode}";
            return RedirectToPage();
        }

        // Handler to delete/hide voucher from customer's list
        public async Task<IActionResult> OnPostHideVoucherAsync(int voucherId)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var voucher = await _context.RedeemedVouchers
                .FirstOrDefaultAsync(v => v.Id == voucherId && v.CustomerId == customerId.Value);

            if (voucher != null)
            {
                _context.RedeemedVouchers.Remove(voucher);
                await _context.SaveChangesAsync();
                StatusMessage = "Voucher removed from your list.";
            }

            return RedirectToPage();
        }
    }

    public class RedeemedVoucher
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string RewardName { get; set; } = "";
        public string PromoCode { get; set; } = "";
        public DateTime RedeemedDate { get; set; } = DateTime.Now;
        public bool IsUsed { get; set; } = false;
    }
}