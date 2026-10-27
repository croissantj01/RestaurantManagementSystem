using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Models;
using RestaurantManagementSystem.Data;

namespace RestaurantManagementSystem.Pages.Admin.RestaurantTables
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<RestaurantTable> RestaurantTables { get; set; }
            = new List<RestaurantTable>();

        public async Task OnGetAsync()
        {
            RestaurantTables = await _context.RestaurantTables
                .ToListAsync();
        }
    }
}