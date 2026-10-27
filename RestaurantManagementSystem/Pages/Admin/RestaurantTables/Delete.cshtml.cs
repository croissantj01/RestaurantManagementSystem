using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Admin.RestaurantTables
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public RestaurantTable RestaurantTable { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            RestaurantTable = await _context.RestaurantTables.FindAsync(id);

            if (RestaurantTable == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var restaurantTable = await _context.RestaurantTables
                .FindAsync(RestaurantTable.RestaurantTableId);

            if (restaurantTable == null)
            {
                return NotFound();
            }

            _context.RestaurantTables.Remove(restaurantTable);

            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}