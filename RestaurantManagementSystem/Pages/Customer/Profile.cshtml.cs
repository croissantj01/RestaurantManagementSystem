using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
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
        private readonly IPasswordHasher<CustomerModel> _hasher;

        public ProfileModel(
            ApplicationDbContext context,
            IPasswordHasher<CustomerModel> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public string DisplayName { get; set; } = "";
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

        // ----- Membership & Loyalty Properties -----
        public bool IsMember { get; set; }
        public string MembershipTier { get; set; } = "Bronze";
        public int LoyaltyPoints { get; set; }
        public DateTime? MemberSince { get; set; }

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
            if (customerId == null) return RedirectToPage("/Customer/Login");

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

            IsMember = customer.IsMember;
            MembershipTier = string.IsNullOrEmpty(customer.MembershipTier) ? "Bronze" : customer.MembershipTier;
            LoyaltyPoints = customer.LoyaltyPoints;
            MemberSince = customer.MemberSince;

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null) return RedirectToPage("/Customer/Login");

            DisplayName = customer.FullName;
            FullName = (FullName ?? "").Trim();
            Email = (Email ?? "").Trim();
            Phone = (Phone ?? "").Trim();

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
                bool emailTaken = await _context.Customers.AnyAsync(c => c.Email == Email);
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
                IsMember = customer.IsMember;
                MembershipTier = customer.MembershipTier;
                LoyaltyPoints = customer.LoyaltyPoints;
                MemberSince = customer.MemberSince;
                return Page();
            }

            customer.FullName = FullName;
            customer.Email = Email;
            customer.Phone = Phone;

            await _context.SaveChangesAsync();
            HttpContext.Session.SetString("CustomerName", customer.FullName);

            StatusMessage = "Your profile has been updated.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Customer/Login");
            }

            // Ignore profile fields during password change validation
            ModelState.Remove(nameof(FullName));
            ModelState.Remove(nameof(Email));
            ModelState.Remove(nameof(Phone));

            DisplayName = customer.FullName;
            FullName = customer.FullName;
            Email = customer.Email;
            Phone = customer.Phone;
            IsMember = customer.IsMember;
            MembershipTier = customer.MembershipTier;
            LoyaltyPoints = customer.LoyaltyPoints;
            MemberSince = customer.MemberSince;

            // Verify current password hash against user input using injected hasher
            var verifyResult = _hasher.VerifyHashedPassword(customer, customer.Password, CurrentPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(nameof(CurrentPassword), "Current password is incorrect.");
            }

            // Standard Password Constraints Validation
            if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 6)
            {
                ModelState.AddModelError(nameof(NewPassword), "New password must be at least 6 characters.");
            }
            else if (!Regex.IsMatch(NewPassword, @"[A-Z]") ||
                     !Regex.IsMatch(NewPassword, @"[a-z]") ||
                     !Regex.IsMatch(NewPassword, @"[0-9]") ||
                     !Regex.IsMatch(NewPassword, @"[\W_]"))
            {
                ModelState.AddModelError(nameof(NewPassword), "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
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

            // Hash the new password using the exact same dependency-injected hasher
            customer.Password = _hasher.HashPassword(customer, NewPassword);
            await _context.SaveChangesAsync();

            StatusMessage = "Your password has been changed successfully.";
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