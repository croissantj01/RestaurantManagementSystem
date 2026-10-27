using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using MenuModel = RestaurantManagementSystem.Models.Menu;

namespace RestaurantManagementSystem.Pages.Menu
{
    public class DeleteMenuModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DeleteMenuModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public MenuModel Menu { get; set; } = new MenuModel();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Check admin login
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            Menu = await _context.Menus
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Menu == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Check admin login
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            // Find menu
            var menu = await _context.Menus
                .FirstOrDefaultAsync(m => m.Id == Menu.Id);

            if (menu == null)
            {
                return NotFound();
            }

            // Delete image from wwwroot
            if (!string.IsNullOrEmpty(menu.ImagePath))
            {
                string imagePath = Path.Combine(
                    _environment.WebRootPath,
                    menu.ImagePath
                        .TrimStart('/')
                        .Replace('/',
                            Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            // Delete menu from database
            _context.Menus.Remove(menu);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Menu deleted successfully!";

            return RedirectToPage("/Admin/Dashboard");
        }
    }
}