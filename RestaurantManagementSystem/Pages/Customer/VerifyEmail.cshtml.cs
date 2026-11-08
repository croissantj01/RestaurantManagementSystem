using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Services;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class VerifyEmailModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public VerifyEmailModel(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [BindProperty]
        public string EnteredOtp { get; set; } = string.Empty;

        public string PendingEmail { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            int? pendingCustomerId = HttpContext.Session.GetInt32("PendingCustomerId");

            if (!pendingCustomerId.HasValue)
            {
                return RedirectToPage("/Customer/Register");
            }

            var customer = await _context.Customers.FindAsync(pendingCustomerId.Value);
            if (customer == null)
            {
                return RedirectToPage("/Customer/Register");
            }

            PendingEmail = customer.Email;
            return Page();
        }

        public async Task<IActionResult> OnPostVerifyAsync()
        {
            int? pendingCustomerId = HttpContext.Session.GetInt32("PendingCustomerId");

            if (!pendingCustomerId.HasValue)
            {
                return RedirectToPage("/Customer/Register");
            }

            var customer = await _context.Customers.FindAsync(pendingCustomerId.Value);

            if (customer == null)
            {
                return RedirectToPage("/Customer/Register");
            }

            PendingEmail = customer.Email;

            // Check Expiry
            if (customer.OtpExpiry == null || DateTime.UtcNow > customer.OtpExpiry)
            {
                ModelState.AddModelError("EnteredOtp", "Verification code has expired. Click 'Resend Code'.");
                return Page();
            }

            // Check Match
            if (string.IsNullOrWhiteSpace(EnteredOtp) || EnteredOtp.Trim() != customer.OtpCode)
            {
                ModelState.AddModelError("EnteredOtp", "Invalid verification code.");
                return Page();
            }

            // Mark customer verified and clear OTP fields
            customer.IsEmailVerified = true;
            customer.OtpCode = null;
            customer.OtpExpiry = null;
            await _context.SaveChangesAsync();

            // Clear pending session state so they log in fresh
            HttpContext.Session.Remove("PendingCustomerId");
            HttpContext.Session.Clear();

            // Set the requested success message in TempData
            TempData["SuccessMessage"] = "Registration success, please login to access the customer dashboard.";

            // Redirect to Index and automatically open the login modal
            return RedirectToPage("/Index", new { login = "true" });
        }

        public async Task<IActionResult> OnPostResendOtpAsync()
        {
            int? pendingCustomerId = HttpContext.Session.GetInt32("PendingCustomerId");

            if (!pendingCustomerId.HasValue)
            {
                return RedirectToPage("/Customer/Register");
            }

            var customer = await _context.Customers.FindAsync(pendingCustomerId.Value);
            if (customer != null)
            {
                string newOtp = new Random().Next(100000, 999999).ToString();
                customer.OtpCode = newOtp;
                customer.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
                await _context.SaveChangesAsync();

                string subject = "MyCafe - New Verification Code";
                string message = $"<h3>Your new verification code is: <b style='color: #f39c12;'>{newOtp}</b></h3><p>This code will expire in 10 minutes.</p>";
                await _emailSender.SendEmailAsync(customer.Email, subject, message);

                TempData["SuccessMessage"] = "A new verification code has been sent!";
            }

            return RedirectToPage("/Customer/VerifyEmail");
        }
    }
}