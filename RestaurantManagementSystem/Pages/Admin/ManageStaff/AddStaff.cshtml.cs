using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using StaffModel = RestaurantManagementSystem.Models.Staff;

namespace RestaurantManagementSystem.Pages.Admin.ManageStaff
{
    public class AddStaffModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AddStaffModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public StaffModel Staff { get; set; } = new StaffModel();

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

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check duplicate username
            if (await _context.Staffs.AnyAsync(s => s.Username == Staff.Username))
            {
                ModelState.AddModelError("Staff.Username", "This username is already taken.");
            }

            // Check duplicate email
            if (await _context.Staffs.AnyAsync(s => s.Email == Staff.Email))
            {
                ModelState.AddModelError("Staff.Email", "This email is already registered.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Staffs.Add(Staff);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Staff account created successfully!";

            return RedirectToPage("/Admin/ManageStaff/ManageStaff");
        }
    }
}