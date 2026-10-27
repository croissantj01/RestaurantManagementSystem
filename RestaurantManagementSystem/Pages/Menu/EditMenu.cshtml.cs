using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using MenuModel = RestaurantManagementSystem.Models.Menu;

namespace RestaurantManagementSystem.Pages.Menu
{
    public class EditMenuModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public EditMenuModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public MenuModel Menu { get; set; } = new MenuModel();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

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

            // Check validation
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Find existing menu
            var existingMenu = await _context.Menus
                .FirstOrDefaultAsync(m => m.Id == Menu.Id);

            if (existingMenu == null)
            {
                return NotFound();
            }

            // Update menu information
            existingMenu.MenuName = Menu.MenuName;
            existingMenu.Description = Menu.Description;
            existingMenu.Price = Menu.Price;

            // Check whether a new image was uploaded
            if (ImageFile != null)
            {
                string imageFolder = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "menu");

                if (!Directory.Exists(imageFolder))
                {
                    Directory.CreateDirectory(imageFolder);
                }

                // Delete old image
                if (!string.IsNullOrEmpty(existingMenu.ImagePath))
                {
                    string oldImagePath = Path.Combine(
                        _environment.WebRootPath,
                        existingMenu.ImagePath
                            .TrimStart('/')
                            .Replace('/',
                                Path.DirectorySeparatorChar));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Generate new file name
                string fileExtension =
                    Path.GetExtension(ImageFile.FileName);

                string fileName =
                    Guid.NewGuid().ToString() + fileExtension;

                string filePath =
                    Path.Combine(imageFolder, fileName);

                // Save new image
                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                // Update image path
                existingMenu.ImagePath =
                    "/images/menu/" + fileName;
            }

            // Save changes
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Menu updated successfully!";

            return RedirectToPage("/Admin/Dashboard");
        }
    }
}