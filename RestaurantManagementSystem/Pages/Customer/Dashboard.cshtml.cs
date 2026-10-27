using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantManagementSystem.Data;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Customer name displayed on dashboard
        public string CustomerName { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            // Check whether customer is logged in
            int? customerId = HttpContext.Session.GetInt32("CustomerId");

            // If not logged in, go to Login page
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            // Get customer name from Session
            CustomerName = HttpContext.Session.GetString("CustomerName")
                ?? "Customer";

            return Page();
        }

        public IActionResult OnPostLogout()
        {
            // Remove all session information
            HttpContext.Session.Clear();

            // Redirect customer to Login page
            return RedirectToPage("/Customer/Login");
        }
    }
}