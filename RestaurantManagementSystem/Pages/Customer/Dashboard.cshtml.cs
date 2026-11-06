using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

        // Dynamic lists for the dashboard
        public IList<RestaurantManagementSystem.Models.Menu> FeaturedMenus { get; set; } = new List<RestaurantManagementSystem.Models.Menu>();
        public IList<RestaurantManagementSystem.Models.Menu> PopularMenus { get; set; } = new List<RestaurantManagementSystem.Models.Menu>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Check whether customer is logged in
            int? customerId = HttpContext.Session.GetInt32("CustomerId");

            // If not logged in, go to Login page
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            // Get customer name from Session
            CustomerName = HttpContext.Session.GetString("CustomerName") ?? "Customer";

            // 1. Fetch Featured Menus for the hero section
            FeaturedMenus = await _context.Menus.Take(4).ToListAsync();

            // 2. Query top ordered menu IDs globally across ALL customer orders
            var popularMenuIds = await _context.OrderItems
                .GroupBy(oi => oi.MenuId)
                .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                .Select(g => g.Key)
                .Take(3)
                .ToListAsync();

            PopularMenus = await _context.Menus
                .Where(m => popularMenuIds.Contains(m.Id))
                .ToListAsync();

            // Sort them to match the exact order of global popularity
            PopularMenus = popularMenuIds
                .Select(id => PopularMenus.FirstOrDefault(m => m.Id == id))
                .Where(m => m != null)
                .ToList()!;

            // Fallback: If no orders have been placed globally yet, grab the first 3 menus as popular items
            if (!PopularMenus.Any())
            {
                PopularMenus = await _context.Menus.Take(3).ToListAsync();
            }

            // Fallback mock items if the database menu table itself is empty
            if (!FeaturedMenus.Any())
            {
                FeaturedMenus.Add(new RestaurantManagementSystem.Models.Menu
                {
                    Id = 1,
                    MenuName = "Macaroon",
                    Description = "Delicious sweet pastry with raspberry/coconut filling.",
                    Price = 12.00m,
                    ImagePath = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=500&auto=format&fit=crop&q=60"
                });
            }

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