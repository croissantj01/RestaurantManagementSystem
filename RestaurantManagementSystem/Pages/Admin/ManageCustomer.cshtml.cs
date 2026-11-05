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
            public List<OrderModel> Orders { get; set; } = new();
            public List<ReservationModel> Reservations { get; set; } = new();
        }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        public List<CustomerRow> Rows { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            var query = _context.Customers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim();
                query = query.Where(c =>
                    c.FullName.Contains(term) || c.Email.Contains(term));
            }

            var customers = await query.OrderBy(c => c.FullName).ToListAsync();
            var ids = customers.Select(c => c.CustomerId).ToList();

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Table)
                .Include(o => o.OrderItems).ThenInclude(i => i.Menu)
                .Where(o => o.CustomerId != null && ids.Contains(o.CustomerId.Value))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var reservations = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.RestaurantTable)
                .Where(r => ids.Contains(r.CustomerId))
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            Rows = customers.Select(c => new CustomerRow
            {
                Customer = c,
                Orders = orders.Where(o => o.CustomerId == c.CustomerId).ToList(),
                Reservations = reservations.Where(r => r.CustomerId == c.CustomerId).ToList()
            }).ToList();

            return Page();
        }
    }
}