using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using StaffModel = RestaurantManagementSystem.Models.Staff;
using MenuModel = RestaurantManagementSystem.Models.Menu;

namespace RestaurantManagementSystem.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Staff list
        public List<StaffModel> StaffList { get; set; }
            = new List<StaffModel>();

        // Menu list
        public List<MenuModel> MenuList { get; set; }
            = new List<MenuModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Check whether admin is logged in
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            // Get all staff accounts
            StaffList = await _context.Staffs
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            // Get all menu items
            MenuList = await _context.Menus
                .OrderBy(m => m.Id)
                .ToListAsync();

            return Page();
        }

        public IActionResult OnPostLogout()
        {
            // Clear admin session
            HttpContext.Session.Clear();

            // Redirect to admin login
            return RedirectToPage("/Admin/Login");
        }
    }
}