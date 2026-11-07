using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;

namespace RestaurantManagementSystem.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<RestaurantManagementSystem.Models.Customer> _hasher;

        public IndexModel(
            ApplicationDbContext context,
            IPasswordHasher<RestaurantManagementSystem.Models.Customer> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        // Hardcoded admin account
        private const string AdminUsername = "admin";
        private const string AdminPassword = "admin";

        [BindProperty]
        public string? LoginEmail { get; set; }

        [BindProperty]
        public string? LoginPassword { get; set; }

        public void OnGet(string? login)
        {
            if (login == "true")
            {
                ViewData["OpenLogin"] = true;
            }
        }

        public async Task<IActionResult> OnPostLoginAsync()
        {
            string input = (LoginEmail ?? "").Trim();
            string password = LoginPassword ?? "";

            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(password))
            {
                return LoginFailed("Please enter your email/username and password.");
            }

            // 1. Admin Login
            if (string.Equals(input, AdminUsername, StringComparison.OrdinalIgnoreCase)
                && password == AdminPassword)
            {
                HttpContext.Session.Clear();
                HttpContext.Session.SetString("AdminLoggedIn", "true");

                return RedirectToPage("/Admin/Dashboard");
            }

            // 2. Staff Login (email or username)
            var staff = await _context.Staffs
                .FirstOrDefaultAsync(s =>
                    s.Email == input || s.Username == input);

            if (staff != null)
            {
                var staffHasher = new PasswordHasher<RestaurantManagementSystem.Models.Staff>();

                bool passwordCorrect;

                // Check whether password is hashed
                if (staff.Password.StartsWith("AQAAAA"))
                {
                    var result = staffHasher.VerifyHashedPassword(
                        staff,
                        staff.Password,
                        password);

                    passwordCorrect =
                        result != PasswordVerificationResult.Failed;
                }
                else
                {
                    // Support existing plain-text staff passwords
                    passwordCorrect = staff.Password == password;

                    // Upgrade existing password to hashed format
                    if (passwordCorrect)
                    {
                        staff.Password = staffHasher.HashPassword(staff, password);
                        await _context.SaveChangesAsync();
                    }
                }

                if (passwordCorrect)
                {
                    HttpContext.Session.Clear();
                    HttpContext.Session.SetString("StaffLoggedIn", "true");
                    HttpContext.Session.SetInt32("StaffId", staff.Id);
                    HttpContext.Session.SetString("StaffName", staff.FullName);

                    return RedirectToPage("/Staff/Dashboard");
                }
            }

            // 3. Customer Login
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == input);

            if (customer != null)
            {
                var result = _hasher.VerifyHashedPassword(
                    customer,
                    customer.Password,
                    password);

                if (result != PasswordVerificationResult.Failed)
                {
                    HttpContext.Session.Clear();
                    HttpContext.Session.SetString("CustomerLoggedIn", "true");
                    HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
                    HttpContext.Session.SetString("CustomerName", customer.FullName);

                    return RedirectToPage("/Customer/Dashboard");
                }
            }

            return LoginFailed("Invalid email/username or password.");
        }

        // Logout Handler
        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Index");
        }

        // Login Error
        private IActionResult LoginFailed(string message)
        {
            ViewData["LoginError"] = message;
            ViewData["LoginEmail"] = LoginEmail;
            ViewData["OpenLogin"] = true;

            return Page();
        }
    }
}
