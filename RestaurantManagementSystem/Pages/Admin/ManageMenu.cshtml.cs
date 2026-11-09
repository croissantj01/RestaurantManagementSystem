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

        // ---- Search + filter (bound from the query string) ----
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CategoryFilter { get; set; }

        // Options for the category dropdown (distinct categories in the database)
        public List<string> Categories { get; set; } = new List<string>();

        // Total number of menu items before any filtering
        public int TotalCount { get; set; }

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
            // Total before filtering (used for the "x of y shown" label)
            TotalCount = await _context.Menus.CountAsync();

            // Distinct categories for the filter dropdown
            Categories = await _context.Menus
                .Select(m => m.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var query = _context.Menus.AsQueryable();

            // Filter by category
            if (!string.IsNullOrWhiteSpace(CategoryFilter))
            {
                query = query.Where(m => m.Category == CategoryFilter);
            }

            // Search by menu name (case-insensitive, partial match)
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(m => m.MenuName.ToLower().Contains(term));
            }

            MenuList = await query
                .OrderBy(m => m.Category)
                .ThenBy(m => m.MenuName)
                .ToListAsync();
        }
    }
}