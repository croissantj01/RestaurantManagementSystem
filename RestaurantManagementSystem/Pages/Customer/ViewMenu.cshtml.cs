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

        public IList<RestaurantManagementSystem.Models.Menu> Menus { get; set; } = default!;

        public async Task OnGetAsync()
        {
            if (_context.Menus != null)
            {
                Menus = await _context.Menus.ToListAsync();
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostAddToCartAsync([FromBody] CartRequestModel data)
        {
            if (data == null)
            {
                return new JsonResult(new { success = false, message = "Invalid data." });
            }

            var menuItem = await _context.Menus.FindAsync(data.MenuId);
            if (menuItem == null)
            {
                return new JsonResult(new { success = false, message = "Menu item not found." });
            }

            var cartItem = new CartItem
            {
                MenuId = menuItem.Id,
                MenuName = menuItem.MenuName,
                Price = menuItem.Price,
                Size = data.Size,
                Quantity = data.Quantity
            };

            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Item added to database cart!" });
        }
    }

    public class CartRequestModel
    {
        public int MenuId { get; set; }
        public string Size { get; set; } = "Small";
        public int Quantity { get; set; }
    }
}