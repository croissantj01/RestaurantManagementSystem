using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class ViewMenuModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ViewMenuModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Models.Menu> Menus { get; set; } = new List<Models.Menu>();
        public List<string> Categories { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            Menus = await _context.Menus
                .OrderBy(m => m.MenuName)
                .ToListAsync();

            // Extract unique categories (ignoring nulls/blanks)
            Categories = Menus
                .Where(m => !string.IsNullOrWhiteSpace(m.Category))
                .Select(m => m.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAddToCartAsync(int menuId, string? size, int quantity)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == menuId);
            if (menu == null)
            {
                TempData["ErrorMessage"] = "Selected menu item was not found.";
                return RedirectToPage();
            }

            string selectedSize = string.IsNullOrWhiteSpace(size) ? "Regular" : size;
            int itemQty = quantity > 0 ? quantity : 1;
            decimal basePrice = menu.Price;

            if (selectedSize.Equals("Large", StringComparison.OrdinalIgnoreCase))
            {
                basePrice += 2.00m;
            }

            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value && c.MenuId == menuId && c.Size == selectedSize);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += itemQty;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CustomerId = customerId.Value,
                    MenuId = menu.Id,
                    MenuName = menu.MenuName,
                    Price = basePrice,
                    Size = selectedSize,
                    Quantity = itemQty
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{menu.MenuName} ({selectedSize}) added to your cart!";

            return RedirectToPage();
        }
    }
}