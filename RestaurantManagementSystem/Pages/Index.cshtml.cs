using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;

namespace RestaurantManagementSystem.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hardcoded admin account (same as the old Admin/Login page)
        private const string AdminUsername = "admin";
        private const string AdminPassword = "admin";

        // Field names match the inputs in the login modal (_HomeLayout.cshtml)
        [BindProperty]
        public string? LoginEmail { get; set; }

        [BindProperty]
        public string? LoginPassword { get; set; }

        // Reports/Dashboard redirect here with ?login=true to open the popup
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

            // 1. Admin
            if (string.Equals(input, AdminUsername, StringComparison.OrdinalIgnoreCase)
                && password == AdminPassword)
            {
                HttpContext.Session.Clear();
                HttpContext.Session.SetString("AdminLoggedIn", "true");
                return RedirectToPage("/Admin/Dashboard");
            }

            // 2. Staff (email or username)
            var staff = await _context.Staffs.AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    (s.Email == input || s.Username == input)
                    && s.Password == password);

            if (staff != null)
            {
                HttpContext.Session.Clear();
                HttpContext.Session.SetString("StaffLoggedIn", "true");
                HttpContext.Session.SetInt32("StaffId", staff.Id);
                HttpContext.Session.SetString("StaffName", staff.FullName);
                return RedirectToPage("/Staff/Dashboard");
            }

            // 3. Customer (ASSUMPTION: Customers table with Email and Password)
            var customer = await _context.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Email == input && c.Password == password);

            if (customer != null)
            {
                HttpContext.Session.Clear();
                HttpContext.Session.SetString("CustomerLoggedIn", "true");
                HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
                HttpContext.Session.SetString("CustomerName", customer.FullName);
                return RedirectToPage("/Customer/Dashboard");
            }

            return LoginFailed("Invalid email/username or password.");
        }

        // Show the homepage again with the popup open and the error message
        private IActionResult LoginFailed(string message)
        {
            ViewData["LoginError"] = message;
            ViewData["LoginEmail"] = LoginEmail;
            ViewData["OpenLogin"] = true;
            return Page();
        }
    }
}