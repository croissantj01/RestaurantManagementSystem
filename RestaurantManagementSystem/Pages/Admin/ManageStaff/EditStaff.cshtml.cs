using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using StaffModel = RestaurantManagementSystem.Models.Staff;

namespace RestaurantManagementSystem.Pages.Admin.ManageStaff
{
    public class EditStaffModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditStaffModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public StaffModel Staff { get; set; } = new StaffModel();

        // Optional: leave blank to keep the current password
        [BindProperty]
        public string? NewPassword { get; set; }

        [BindProperty]
        public string? ConfirmNewPassword { get; set; }

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

            // Password fields are handled separately on the Edit page
            ModelState.Remove("Staff.Password");
            ModelState.Remove("Staff.ConfirmPassword");

            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                if (NewPassword.Length < 6)
                {
                    ModelState.AddModelError("NewPassword", "Password must be at least 6 characters.");
                }

                if (NewPassword != ConfirmNewPassword)
                {
                    ModelState.AddModelError("ConfirmNewPassword", "Passwords do not match.");
                }
            }

            // Check duplicates against other staff
            if (await _context.Staffs.AnyAsync(s => s.Username == Staff.Username && s.Id != Staff.Id))
            {
                ModelState.AddModelError("Staff.Username", "This username is already taken.");
            }

            if (await _context.Staffs.AnyAsync(s => s.Email == Staff.Email && s.Id != Staff.Id))
            {
                ModelState.AddModelError("Staff.Email", "This email is already registered.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingStaff = await _context.Staffs
                .FirstOrDefaultAsync(s => s.Id == Staff.Id);

            if (existingStaff == null)
            {
                return NotFound();
            }

            existingStaff.FullName = Staff.FullName;
            existingStaff.Email = Staff.Email;
            existingStaff.Username = Staff.Username;

            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                existingStaff.Password = NewPassword;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Staff account updated successfully!";

            return RedirectToPage("/Admin/ManageStaff/ManageStaff");
        }
    }
}