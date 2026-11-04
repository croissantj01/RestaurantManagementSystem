using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using StaffModel = RestaurantManagementSystem.Models.Staff;

namespace RestaurantManagementSystem.Pages.Admin.ManageStaff
{
    public class DeleteStaffModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteStaffModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public StaffModel Staff { get; set; } = new StaffModel();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Check admin login
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            var staff = await _context.Staffs
                .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound();
            }

            Staff = staff;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Check admin login
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            var staff = await _context.Staffs
                .FirstOrDefaultAsync(s => s.Id == Staff.Id);

            if (staff == null)
            {
                return NotFound();
            }

            _context.Staffs.Remove(staff);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Staff account deleted successfully!";

            return RedirectToPage("/Admin/ManageStaff/ManageStaff");
        }
    }
}