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

        [BindProperty]
        public MenuModel NewMenu { get; set; } = new MenuModel();

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check admin login
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            await LoadMenuListAsync();
            return Page();
        }

        // Handles standard form posts
        public async Task<IActionResult> OnPostAsync()
        {
            return await HandleMenuCreationAsync();
        }

        // Handles form posts if your HTML has asp-page-handler="Create"
        public async Task<IActionResult> OnPostCreateAsync()
        {
            return await HandleMenuCreationAsync();
        }

        // Centralized logic to block duplicates globally
        private async Task<IActionResult> HandleMenuCreationAsync()
        {
            // Check admin login
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            string trimmedName = (NewMenu.MenuName ?? "").Trim();

            // GLOBAL CHECK: Check if ANY item in the database already has this name
            bool itemExists = await _context.Menus
                .AnyAsync(m => m.MenuName.ToLower() == trimmedName.ToLower());

            if (itemExists)
            {
                ModelState.AddModelError("NewMenu.MenuName", $"The menu item '{trimmedName}' already exists. Duplicate names are not allowed.");
            }

            if (!ModelState.IsValid)
            {
                // Reload the list so the page renders properly with existing items and error messages
                await LoadMenuListAsync();
                return Page();
            }

            NewMenu.MenuName = trimmedName;
            _context.Menus.Add(NewMenu);
            await _context.SaveChangesAsync();

            StatusMessage = $"Menu item '{trimmedName}' added successfully.";
            return RedirectToPage();
        }

        private async Task LoadMenuListAsync()
        {
            MenuList = await _context.Menus
                .OrderBy(m => m.Category)
                .ThenBy(m => m.MenuName)
                .ToListAsync();
        }
    }
}