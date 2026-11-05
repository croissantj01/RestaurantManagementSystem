using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using CustomerModel = RestaurantManagementSystem.Models.Customer;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProfileModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Name shown in the navbar and avatar (always from the database)
        public string DisplayName { get; set; } = "";

        // Set to true when the password form has errors so it stays open
        public bool PasswordError { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        // ----- Basic info form -----
        [BindProperty]
        public string FullName { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string? Phone { get; set; }

        // ----- Change password form -----
        [BindProperty]
        public string CurrentPassword { get; set; } = "";

        [BindProperty]
        public string NewPassword { get; set; } = "";

        [BindProperty]
        public string ConfirmNewPassword { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Customer/Login");
            }

            DisplayName = customer.FullName;
            FullName = customer.FullName;
            Email = customer.Email;
            Phone = customer.Phone;

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Customer/Login");
            }

            DisplayName = customer.FullName;

            FullName = (FullName ?? "").Trim();
            Email = (Email ?? "").Trim();
            Phone = (Phone ?? "").Trim();

            // ----- Validation -----
            if (string.IsNullOrWhiteSpace(FullName))
            {
                ModelState.AddModelError(nameof(FullName), "Please enter your name.");
            }
            else if (FullName.Length > 100)
            {
                ModelState.AddModelError(nameof(FullName), "Name is too long.");
            }

            if (!IsValidEmail(Email))
            {
                ModelState.AddModelError(nameof(Email), "Please enter a valid email address.");
            }
            else if (!string.Equals(Email, customer.Email, StringComparison.OrdinalIgnoreCase))
            {
                bool emailTaken = await _context.Customers
                    .AnyAsync(c => c.Email == Email);

                if (emailTaken)
                {
                    ModelState.AddModelError(nameof(Email), "That email is already used by another account.");
                }
            }

            if (!string.IsNullOrEmpty(Phone) && !Regex.IsMatch(Phone, @"^[0-9+\-\s]{8,15}$"))
            {
                ModelState.AddModelError(nameof(Phone), "Please enter a valid phone number.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // ----- Save -----
            customer.FullName = FullName;
            customer.Email = Email;
            customer.Phone = Phone;

            await _context.SaveChangesAsync();

            // Keep the session name in sync
            HttpContext.Session.SetString("CustomerName", customer.FullName);

            StatusMessage = "Your profile has been updated.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Customer/Login");
            }

            // Re-fill the basic info form with saved values
            DisplayName = customer.FullName;
            FullName = customer.FullName;
            Email = customer.Email;
            Phone = customer.Phone;

            if (customer.Password != CurrentPassword)
            {
                ModelState.AddModelError(nameof(CurrentPassword), "Current password is incorrect.");
            }

            if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 6)
            {
                ModelState.AddModelError(nameof(NewPassword), "New password must be at least 6 characters.");
            }
            else if (NewPassword == CurrentPassword)
            {
                ModelState.AddModelError(nameof(NewPassword), "New password must be different from the current one.");
            }

            if (NewPassword != ConfirmNewPassword)
            {
                ModelState.AddModelError(nameof(ConfirmNewPassword), "Passwords do not match.");
            }

            if (!ModelState.IsValid)
            {
                PasswordError = true;
                return Page();
            }

            customer.Password = NewPassword;
            await _context.SaveChangesAsync();

            StatusMessage = "Your password has been changed.";
            return RedirectToPage();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Customer/Login");
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}