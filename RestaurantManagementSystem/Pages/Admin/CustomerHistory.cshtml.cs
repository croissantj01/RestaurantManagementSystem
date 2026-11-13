using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;
using CustomerModel = RestaurantManagementSystem.Models.Customer;

namespace RestaurantManagementSystem.Pages.Admin
{
    public class CustomerHistoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CustomerHistoryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public CustomerModel Customer { get; set; } = null!;
        public List<Order> Orders { get; set; } = new();
        public List<Reservation> Reservations { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageCount { get; set; }

        [BindProperty(SupportsGet = true)] public int CustomerId { get; set; }
        [BindProperty(SupportsGet = true)] public string Type { get; set; } = "orders";
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        private const int PageSize = 10;

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
                return RedirectToPage("/Admin/Login");

            Customer = await _context.Customers
    .AsNoTracking()
    .FirstOrDefaultAsync(c => c.CustomerId == CustomerId);

            if (Customer == null)
                return NotFound();

            Type = Type?.ToLowerInvariant() == "reservations" ? "reservations" : "orders";
            PageNumber = Math.Max(1, PageNumber);

            if (Type == "orders")
            {
                var query = _context.Orders.AsNoTracking()
                    .Include(o => o.Table)
                    .Include(o => o.OrderItems).ThenInclude(i => i.Menu)
                    .Where(o => o.CustomerId == CustomerId);

                if (int.TryParse(Search?.Trim(), out var orderId))
                    query = query.Where(o => o.OrderId == orderId);
                else if (!string.IsNullOrWhiteSpace(Search))
                    query = query.Where(o => false);

                if (FromDate.HasValue)
                    query = query.Where(o => o.OrderDate >= FromDate.Value.Date);
                if (ToDate.HasValue)
                    query = query.Where(o => o.OrderDate < ToDate.Value.Date.AddDays(1));

                TotalRecords = await query.CountAsync();
                PageCount = (int)Math.Ceiling(TotalRecords / (double)PageSize);
                Orders = await query.OrderByDescending(o => o.OrderDate)
                    .Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync();
            }
            else
            {
                var query = _context.Reservations.AsNoTracking()
                    .Include(r => r.RestaurantTable)
                    .Where(r => r.CustomerId == CustomerId);

                if (int.TryParse(Search?.Trim(), out var reservationId))
                    query = query.Where(r => r.ReservationId == reservationId);
                else if (!string.IsNullOrWhiteSpace(Search))
                    query = query.Where(r => false);

                if (FromDate.HasValue)
                    query = query.Where(r => r.ReservationDate >= FromDate.Value.Date);
                if (ToDate.HasValue)
                    query = query.Where(r => r.ReservationDate < ToDate.Value.Date.AddDays(1));

                TotalRecords = await query.CountAsync();
                PageCount = (int)Math.Ceiling(TotalRecords / (double)PageSize);
                Reservations = await query.OrderByDescending(r => r.ReservationDate)
                    .ThenByDescending(r => r.ReservationTime)
                    .Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync();
            }

            if (PageCount > 0 && PageNumber > PageCount)
                return RedirectToPage(new { CustomerId, Type, Search, FromDate, ToDate, PageNumber = PageCount });

            return Page();
        }
    }
}
