using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantManagementSystem.Data;
using MenuModel = RestaurantManagementSystem.Models.Menu;

namespace RestaurantManagementSystem.Pages.Menu
{
    public class AddMenuModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AddMenuModel(
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

        public IActionResult OnGet()
        {
            // Check admin login
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
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

            // Check image
            if (ImageFile == null)
            {
                ModelState.AddModelError(
                    "ImageFile",
                    "Please select a menu image.");

                return Page();
            }

            // Create image folder
            string imageFolder = Path.Combine(
                _environment.WebRootPath,
                "images",
                "menu");

            if (!Directory.Exists(imageFolder))
            {
                Directory.CreateDirectory(imageFolder);
            }

            // Get file extension
            string fileExtension =
                Path.GetExtension(ImageFile.FileName);

            // Create unique file name
            string fileName =
                Guid.NewGuid().ToString() + fileExtension;

            string filePath =
                Path.Combine(imageFolder, fileName);

            // Save image
            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await ImageFile.CopyToAsync(stream);
            }

            // Store image path in database
            Menu.ImagePath =
                "/images/menu/" + fileName;

            // Add menu
            _context.Menus.Add(Menu);

            // Save to database
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Menu added successfully!";

            return RedirectToPage("/Admin/Dashboard");
        }
    }
}