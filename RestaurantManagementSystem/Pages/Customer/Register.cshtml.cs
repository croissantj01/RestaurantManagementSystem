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
        private readonly IPasswordHasher<global::RestaurantManagementSystem.Models.Customer> _hasher;

        public RegisterModel(
            ApplicationDbContext context,
            IPasswordHasher<global::RestaurantManagementSystem.Models.Customer> hasher)
        {
            _context = context;
            _hasher = hasher;
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

            // Hash password securely
            Customer.Password = _hasher.HashPassword(Customer, Customer.Password);

            _context.Customers.Add(Customer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "You can now login with your registered account.";
            return RedirectToPage("/Index");
        }
    }
}