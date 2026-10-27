using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using StaffModel = RestaurantManagementSystem.Models.Staff;

namespace RestaurantManagementSystem.Pages.Staff
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegisterModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public StaffModel Staff { get; set; } = new StaffModel();

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

            // Check duplicate username
            bool usernameExists = await _context.Staffs
                .AnyAsync(s => s.Username == Staff.Username);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    "Staff.Username",
                    "This username already exists.");

                return Page();
            }

            // Check duplicate email
            bool emailExists = await _context.Staffs
                .AnyAsync(s => s.Email == Staff.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Staff.Email",
                    "This email already exists.");

                return Page();
            }

            // Add staff
            _context.Staffs.Add(Staff);

            // Save to database
            await _context.SaveChangesAsync();

            // Success message
            TempData["SuccessMessage"] =
                "Staff account created successfully!";

            return RedirectToPage("/Staff/Register");
        }
    }
}