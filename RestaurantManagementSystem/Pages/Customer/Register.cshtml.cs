using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<RestaurantManagementSystem.Models.Customer> _hasher;

        public RegisterModel(
            ApplicationDbContext context,
            IPasswordHasher<RestaurantManagementSystem.Models.Customer> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        [BindProperty]
        public RestaurantManagementSystem.Models.Customer Customer { get; set; } = new RestaurantManagementSystem.Models.Customer();

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Check password confirmation
            if (Customer.Password != ConfirmPassword)
            {
                ModelState.AddModelError(
                    "ConfirmPassword",
                    "Passwords do not match."
                );
            }

            // Check whether email already exists
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == Customer.Email);

            if (existingCustomer != null)
            {
                ModelState.AddModelError(
                    "Customer.Email",
                    "This email is already registered."
                );
            }

            // Check validation
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Hash the password before saving
            Customer.Password = _hasher.HashPassword(Customer, Customer.Password);

            // Add customer to database
            _context.Customers.Add(Customer);
            await _context.SaveChangesAsync();

            // Success message
            TempData["SuccessMessage"] =
                "You can now login with your registered account.";

            // Go to Login page
            return RedirectToPage("/Customer/Login");
        }
    }
}