using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Services;

namespace RestaurantManagementSystem.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<RestaurantManagementSystem.Models.Customer> _hasher;
        private readonly IEmailSender _emailSender;

        public IndexModel(
            ApplicationDbContext context,
            IPasswordHasher<RestaurantManagementSystem.Models.Customer> hasher,
            IEmailSender emailSender)
        {
            _context = context;
            _hasher = hasher;
            _emailSender = emailSender;
        }

        // Hardcoded admin account
        private const string AdminUsername = "admin";
        private const string AdminPassword = "admin";

        [BindProperty]
        public string? LoginEmail { get; set; }

        [BindProperty]
        public string? LoginPassword { get; set; }

        [BindProperty]
        public string? LoginOtp { get; set; }

        // Helper property to toggle UI between Password mode and OTP mode in Modal
        public bool RequireOtp => HttpContext.Session.GetInt32("PendingCustomerId") != null;

        public void OnGet(string? login)
        {
            if (login == "true")
            {
                ViewData["OpenLogin"] = true;
            }

            // Capture success messages passed from other pages (e.g., registration verification)
            if (TempData["SuccessMessage"] != null)
            {
                ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                ViewData["OpenLogin"] = true; // Automatically open the login modal to display it
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

            // 1. Admin Login (Bypasses OTP)
            if (string.Equals(input, AdminUsername, StringComparison.OrdinalIgnoreCase)
                && password == AdminPassword)
            {
                HttpContext.Session.Clear();
                HttpContext.Session.SetString("AdminLoggedIn", "true");

                return RedirectToPage("/Admin/Dashboard");
            }

            // 2. Staff Login (Bypasses OTP)
            var staff = await _context.Staffs
                .FirstOrDefaultAsync(s => s.Email == input || s.Username == input);

            if (staff != null)
            {
                var staffHasher = new PasswordHasher<RestaurantManagementSystem.Models.Staff>();
                bool passwordCorrect;

                if (staff.Password.StartsWith("AQAAAA"))
                {
                    var result = staffHasher.VerifyHashedPassword(staff, staff.Password, password);
                    passwordCorrect = result != PasswordVerificationResult.Failed;
                }
                else
                {
                    passwordCorrect = staff.Password == password;
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

            // 3. Customer Login with OTP Verification Flow
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == input);

            if (customer != null)
            {
                var result = _hasher.VerifyHashedPassword(customer, customer.Password, password);

                if (result != PasswordVerificationResult.Failed)
                {
                    // Generate 6-digit OTP
                    string otp = new Random().Next(100000, 999999).ToString();

                    customer.OtpCode = otp;
                    customer.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
                    await _context.SaveChangesAsync();

                    // Store pending Customer ID in Session
                    HttpContext.Session.SetInt32("PendingCustomerId", customer.CustomerId);

                    // Send Email
                    string subject = "MyCafe - Login Verification Code";
                    string message = $"<h3>Your login verification code is: <b style='color: #f39c12;'>{otp}</b></h3><p>This code will expire in 10 minutes.</p>";
                    await _emailSender.SendEmailAsync(customer.Email, subject, message);

                    ViewData["OpenLogin"] = true;
                    ViewData["SuccessMessage"] = "Verification code sent to your email!";
                    return Page();
                }
            }

            return LoginFailed("Invalid email/username or password.");
        }

        // Verify Customer Login OTP
        public async Task<IActionResult> OnPostVerifyLoginOtpAsync()
        {
            int? pendingId = HttpContext.Session.GetInt32("PendingCustomerId");

            if (pendingId == null)
            {
                return LoginFailed("Session expired. Please enter your email and password again.");
            }

            var customer = await _context.Customers.FindAsync(pendingId.Value);

            if (customer == null)
            {
                HttpContext.Session.Remove("PendingCustomerId");
                return LoginFailed("Customer not found. Please log in again.");
            }

            // Check Expiry
            if (customer.OtpExpiry == null || DateTime.UtcNow > customer.OtpExpiry)
            {
                HttpContext.Session.Remove("PendingCustomerId");
                return LoginFailed("Verification code has expired. Please log in again.");
            }

            // Check Match
            if (string.IsNullOrWhiteSpace(LoginOtp) || LoginOtp.Trim() != customer.OtpCode)
            {
                ViewData["OpenLogin"] = true;
                ModelState.AddModelError("LoginOtp", "Invalid verification code.");
                return Page();
            }

            // Clear OTP fields
            customer.OtpCode = null;
            customer.OtpExpiry = null;
            await _context.SaveChangesAsync();

            // Establish Full Session
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("CustomerLoggedIn", "true");
            HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
            HttpContext.Session.SetString("CustomerName", customer.FullName);

            return RedirectToPage("/Customer/Dashboard");
        }

        // Cancel / Reset OTP state
        public IActionResult OnPostCancelOtp()
        {
            HttpContext.Session.Remove("PendingCustomerId");
            return RedirectToPage("/Index", new { login = "true" });
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