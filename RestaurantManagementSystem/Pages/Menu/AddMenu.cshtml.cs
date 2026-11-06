using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using MenuModel = RestaurantManagementSystem.Models.Menu;

namespace RestaurantManagementSystem.Pages.Menu
{
    public class AddMenuModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AddMenuModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public MenuModel Menu { get; set; } = new MenuModel();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            string trimmedName = (Menu?.MenuName ?? "").Trim();

            // 1. GLOBAL DUPLICATE NAME CHECK (Case-insensitive)
            bool itemExists = await _context.Menus
                .AnyAsync(m => m.MenuName.ToLower() == trimmedName.ToLower());

            if (itemExists)
            {
                ModelState.AddModelError("Menu.MenuName", $"The menu item '{trimmedName}' already exists. Duplicate names are not allowed.");
            }

            // 2. IMAGE FILE VALIDATION CONSTRAINTS (Max 15MB)
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Constraint A: Verify file type is an image
                if (!ImageFile.ContentType.StartsWith("image/"))
                {
                    ModelState.AddModelError("ImageFile", "Please upload a valid image file (e.g., JPG, PNG, WEBP).");
                }

                // Constraint B: Verify file size does not exceed 15MB
                long maxFileSize = 15 * 1024 * 1024; // 15 MB
                if (ImageFile.Length > maxFileSize)
                {
                    ModelState.AddModelError("ImageFile", "The image file is too large. Maximum allowed size is 15MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 3. PROCESS & SAVE IMAGE FILE SAFELY
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Safe fallback for WebRootPath to prevent loading exceptions
                string webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string uploadsFolder = Path.Combine(webRootPath, "images", "menu");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(fileStream);
                }

                Menu.ImagePath = "/images/menu/" + uniqueFileName;
            }

            // 4. SAVE TO DATABASE
            Menu.MenuName = trimmedName;
            _context.Menus.Add(Menu);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Menu item '{trimmedName}' added successfully.";
            return RedirectToPage("/Admin/ManageMenu");
        }
    }
}