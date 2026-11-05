using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Staff
{
    public class OrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public OrdersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Order> Orders { get; set; } = new List<Order>();

        // Summary counts
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int PreparingOrders { get; set; }
        public int CompletedOrders { get; set; }

        // Search and filter
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        public async Task OnGetAsync()
        {
            // Summary counts
            TotalOrders = await _context.Orders.CountAsync();

            PendingOrders = await _context.Orders
                .CountAsync(o => o.Status == "Pending");

            PreparingOrders = await _context.Orders
                .CountAsync(o => o.Status == "Preparing");

            CompletedOrders = await _context.Orders
                .CountAsync(o => o.Status == "Completed");

            // Retrieve orders
            var query = _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Menu)
                .AsQueryable();

            // Search by Order ID or Table Number
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var search = SearchTerm.Trim();

                if (int.TryParse(search, out int orderId))
                {
                    query = query.Where(o =>
                        o.OrderId == orderId ||
                        (o.Table != null &&
                         o.Table.TableNumber.Contains(search))
                    );
                }
                else
                {
                    query = query.Where(o =>
                        o.Table != null &&
                        o.Table.TableNumber.Contains(search)
                    );
                }
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(SelectedStatus))
            {
                query = query.Where(o => o.Status == SelectedStatus);
            }

            // Filter by order date
            if (SelectedDate.HasValue)
            {
                var date = SelectedDate.Value.Date;

                query = query.Where(o =>
                    o.OrderDate.Date == date);
            }

            Orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        // Update order status
        public async Task<IActionResult> OnPostUpdateStatusAsync(
            int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            var allowedStatuses = new[]
            {
                "Pending",
                "Preparing",
                "Completed",
                "Cancelled"
            };

            if (!allowedStatuses.Contains(status))
            {
                return BadRequest();
            }

            order.Status = status;

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}


