using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using StaffModel = RestaurantManagementSystem.Models.Staff;

namespace RestaurantManagementSystem.Pages.Admin.ManageStaff
{
    public class ManageStaffModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageStaffModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<StaffModel> StaffList { get; set; } = new List<StaffModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Check admin login
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            StaffList = await _context.Staffs
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            return Page();
        }
    }
}