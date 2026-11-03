using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using MenuModel = RestaurantManagementSystem.Models.Menu;

namespace RestaurantManagementSystem.Pages.Admin
{
    public class ManageMenuModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageMenuModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<MenuModel> MenuList { get; set; } = new List<MenuModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Check admin login
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            MenuList = await _context.Menus
                .OrderBy(m => m.Category)
                .ThenBy(m => m.MenuName)
                .ToListAsync();

            return Page();
        }
    }
}