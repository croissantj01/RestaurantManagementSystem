using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [TempData]
        public string? SuccessMessage { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter your email and password.";
                return Page();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == Email &&
                                          c.Password == Password);

            if (customer == null)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            // Store Customer ID in Session
            HttpContext.Session.SetInt32(
                "CustomerId",
                customer.CustomerId
            );

            // Store Customer Full Name in Session
            HttpContext.Session.SetString(
                "CustomerName",
                customer.FullName
            );

            // Go to Customer Dashboard
            return RedirectToPage("/Customer/Dashboard");
        }
    }
}