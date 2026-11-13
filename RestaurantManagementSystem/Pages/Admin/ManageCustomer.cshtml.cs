using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using CustomerModel = RestaurantManagementSystem.Models.Customer;
using OrderModel = RestaurantManagementSystem.Models.Order;
using ReservationModel = RestaurantManagementSystem.Models.Reservation;

namespace RestaurantManagementSystem.Pages.Admin
{
    public class ManageCustomerModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageCustomerModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class CustomerRow
        {
            public CustomerModel Customer { get; set; } = null!;
            public List<OrderModel> RecentOrders { get; set; } = new();
            public List<ReservationModel> RecentReservations { get; set; } = new();
            public int OrderCount { get; set; }
            public int ReservationCount { get; set; }
        }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        public List<CustomerRow> Rows { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
                return RedirectToPage("/Admin/Login");

            var customerQuery = _context.Customers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim();
                customerQuery = customerQuery.Where(c =>
                    c.FullName.Contains(term) || c.Email.Contains(term));
            }

            var customers = await customerQuery.OrderBy(c => c.FullName).ToListAsync();
            var customerIds = customers.Select(c => c.CustomerId).ToList();

            // Load order and reservation records once, then show only the latest three
            // in each customer's preview. The full history is on CustomerHistory.
            var allOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Table)
                .Include(o => o.OrderItems).ThenInclude(i => i.Menu)
                .Where(o => o.CustomerId != null && customerIds.Contains(o.CustomerId.Value))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var allReservations = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.RestaurantTable)
                .Where(r => customerIds.Contains(r.CustomerId))
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            Rows = customers.Select(c =>
            {
                var customerOrders = allOrders.Where(o => o.CustomerId == c.CustomerId).ToList();
                var customerReservations = allReservations.Where(r => r.CustomerId == c.CustomerId).ToList();

                return new CustomerRow
                {
                    Customer = c,
                    OrderCount = customerOrders.Count,
                    ReservationCount = customerReservations.Count,
                    RecentOrders = customerOrders.Take(3).ToList(),
                    RecentReservations = customerReservations.Take(3).ToList()
                };
            }).ToList();

            return Page();
        }
    }
}
