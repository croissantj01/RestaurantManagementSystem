using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Services;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<global::RestaurantManagementSystem.Models.Customer> _hasher;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            ApplicationDbContext context,
            IPasswordHasher<global::RestaurantManagementSystem.Models.Customer> hasher,
            IEmailSender emailSender)
        {
            _context = context;
            _hasher = hasher;
            _emailSender = emailSender;
        }

        [BindProperty]
        public global::RestaurantManagementSystem.Models.Customer Customer { get; set; } = new global::RestaurantManagementSystem.Models.Customer();

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Customer.Password != ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
            }

            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == Customer.Email);

            if (existingCustomer != null)
            {
                ModelState.AddModelError("Customer.Email", "This email is already registered.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Generate a secure 6-digit random code
            string otp = new Random().Next(100000, 999999).ToString();

            // Hash password securely
            Customer.Password = _hasher.HashPassword(Customer, Customer.Password);
            Customer.OtpCode = otp;
            Customer.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            Customer.IsEmailVerified = false;

            _context.Customers.Add(Customer);
            await _context.SaveChangesAsync();

            // Store pending customer ID in session for the verification page
            HttpContext.Session.SetInt32("PendingCustomerId", Customer.CustomerId);

            // Send Email
            string subject = "MyCafe - Email Verification Code";
            string message = $"<h3>Your verification code is: <b style='color: #f39c12;'>{otp}</b></h3><p>This code will expire in 10 minutes.</p>";
            await _emailSender.SendEmailAsync(Customer.Email, subject, message);

            // Redirect to the dedicated Verification Page
            return RedirectToPage("/Customer/VerifyEmail");
        }
    }
}