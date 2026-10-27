using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Models;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Orders
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public IList<Order> Orders { get; set; }
            = new List<Order>();

        public HashSet<int> CurrentOrderIds { get; set; }
            = new HashSet<int>();

        public async Task OnGetAsync()
        {
            // Load all orders
            Orders = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Menu)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var latestOrders = Orders
                .Where(o => o.TableId > 0)
                .GroupBy(o => o.TableId)
                .Select(g => g
                    .OrderByDescending(o => o.OrderDate)
                    .First())
                .ToList();

            foreach (var order in latestOrders)
            {
                CurrentOrderIds.Add(order.OrderId);
            }
        }

        public async Task<IActionResult> OnPostMakeAvailableAsync(int id)
        {
            // Find the selected order
            var order = await _context.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Table != null)
            {
                order.Table.Status = "Available";
            }

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}