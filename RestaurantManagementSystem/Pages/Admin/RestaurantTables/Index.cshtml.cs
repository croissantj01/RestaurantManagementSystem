using Microsoft.AspNetCore.Mvc;
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

        // ---- Filter by dining area (bound from the query string) ----
        [BindProperty(SupportsGet = true)]
        public string? AreaFilter { get; set; }

        // Options for the dining area dropdown (distinct areas in the database)
        public List<string> DiningAreas { get; set; } = new List<string>();

        // Total number of tables before any filtering
        public int TotalCount { get; set; }

        public async Task OnGetAsync()
        {
            TotalCount = await _context.RestaurantTables.CountAsync();

            DiningAreas = await _context.RestaurantTables
                .Where(t => t.DiningArea != null && t.DiningArea != "")
                .Select(t => t.DiningArea)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            var query = _context.RestaurantTables.AsQueryable();

            if (!string.IsNullOrWhiteSpace(AreaFilter))
            {
                query = query.Where(t => t.DiningArea == AreaFilter);
            }

            RestaurantTables = await query.ToListAsync();
        }
    }
}