using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using StaffModel = RestaurantManagementSystem.Models.Staff;

namespace RestaurantManagementSystem.Pages.Staff
{
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<StaffModel> _hasher;

        public ProfileModel(ApplicationDbContext context)
        {
            _context = context;
            _hasher = new PasswordHasher<StaffModel>();
        }

        // Profile information
        public class ProfileInput
        {
            [Required(ErrorMessage = "Full Name is required.")]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = "";

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Please enter a valid email.")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Username is required.")]
            public string Username { get; set; } = "";
        }

        [BindProperty]
        public ProfileInput Profile { get; set; } = new();

        // Change password
        public class PasswordInput
        {
            [Required(ErrorMessage = "Current password is required.")]
            [DataType(DataType.Password)]
            public string CurrentPassword { get; set; } = "";

            [Required(ErrorMessage = "New password is required.")]
            [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; } = "";

            [Required(ErrorMessage = "Please confirm your new password.")]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
            [DataType(DataType.Password)]
            public string ConfirmNewPassword { get; set; } = "";
        }

        [BindProperty]
        public PasswordInput Password { get; set; } = new();

        // Get logged-in staff
        private async Task<StaffModel?> GetLoggedInStaff()
        {
            int? staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null)
                return null;

            return await _context.Staffs
                .FirstOrDefaultAsync(s => s.Id == staffId.Value);
        }

        // Load profile page
        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("StaffLoggedIn") != "true")
                return RedirectToPage("/Index");

            var staff = await GetLoggedInStaff();

            if (staff == null)
                return RedirectToPage("/Index");

            LoadProfile(staff);

            return Page();
        }

        // Update personal information
        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            if (HttpContext.Session.GetString("StaffLoggedIn") != "true")
                return RedirectToPage("/Index");

            var staff = await GetLoggedInStaff();

            if (staff == null)
                return RedirectToPage("/Index");

            // Remove password validation errors
            var passwordKeys = ModelState.Keys
                .Where(k => k.StartsWith("Password"))
                .ToList();

            foreach (var key in passwordKeys)
                ModelState.Remove(key);

            if (!ModelState.IsValid)
                return ReloadProfile(staff);

            Profile.FullName = Profile.FullName.Trim();
            Profile.Email = Profile.Email.Trim();
            Profile.Username = Profile.Username.Trim();

            // Check duplicate email
            bool emailExists = await _context.Staffs.AnyAsync(s =>
                s.Email == Profile.Email && s.Id != staff.Id);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Profile.Email",
                    "This email is already registered.");
            }

            // Check duplicate username
            bool usernameExists = await _context.Staffs.AnyAsync(s =>
                s.Username == Profile.Username && s.Id != staff.Id);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    "Profile.Username",
                    "This username is already taken.");
            }

            if (!ModelState.IsValid)
                return ReloadProfile(staff);

            staff.FullName = Profile.FullName;
            staff.Email = Profile.Email;
            staff.Username = Profile.Username;

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("StaffName", staff.FullName);

            TempData["ProfileSuccess"] =
                "Your profile has been updated successfully!";

            return RedirectToPage();
        }

        // Change password
        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            if (HttpContext.Session.GetString("StaffLoggedIn") != "true")
                return RedirectToPage("/Index");

            var staff = await GetLoggedInStaff();

            if (staff == null)
                return RedirectToPage("/Index");

            // Reload existing profile information
            LoadProfile(staff);

            // Clear validation errors from other fields
            ModelState.Clear();

            // Validate password fields only
            if (!TryValidateModel(Password, "Password"))
                return Page();

            // Verify current password
            bool passwordCorrect;

            if (staff.Password.StartsWith("AQAAAA"))
            {
                var result = _hasher.VerifyHashedPassword(
                    staff,
                    staff.Password,
                    Password.CurrentPassword);

                passwordCorrect =
                    result != PasswordVerificationResult.Failed;
            }
            else
            {
                // Support existing plain-text passwords
                passwordCorrect =
                    staff.Password == Password.CurrentPassword;
            }

            if (!passwordCorrect)
            {
                ModelState.AddModelError(
                    "Password.CurrentPassword",
                    "Your current password is incorrect.");

                return Page();
            }

            // Prevent using the same password
            if (Password.NewPassword == Password.CurrentPassword)
            {
                ModelState.AddModelError(
                    "Password.NewPassword",
                    "New password must be different from your current password.");

                return Page();
            }

            // Hash and update the new password
            staff.Password = _hasher.HashPassword(
                staff,
                Password.NewPassword);

            await _context.SaveChangesAsync();

            TempData["PasswordSuccess"] =
                "Your password has been changed successfully!";

            return RedirectToPage();
        }

        // Load profile information
        private void LoadProfile(StaffModel staff)
        {
            Profile.FullName = staff.FullName;
            Profile.Email = staff.Email;
            Profile.Username = staff.Username;
        }

        // Reload profile after validation errors
        private IActionResult ReloadProfile(StaffModel staff)
        {
            LoadProfile(staff);
            return Page();
        }
    }
}



