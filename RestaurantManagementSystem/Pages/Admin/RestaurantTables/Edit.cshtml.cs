using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Admin.RestaurantTables
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public RestaurantTable RestaurantTable { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            RestaurantTable = await _context.RestaurantTables
                .FindAsync(id);

            if (RestaurantTable == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check whether another table already uses this table number
            bool tableExists = await _context.RestaurantTables
                .AnyAsync(t =>
                    t.TableNumber == RestaurantTable.TableNumber &&
                    t.RestaurantTableId != RestaurantTable.RestaurantTableId);

            if (tableExists)
            {
                ModelState.AddModelError(
                    "RestaurantTable.TableNumber",
                    "This table number already exists."
                );

                return Page();
            }

            // Find the existing table
            var existingTable = await _context.RestaurantTables
                .FindAsync(RestaurantTable.RestaurantTableId);

            if (existingTable == null)
            {
                return NotFound();
            }

            // Update the table information
            existingTable.TableNumber = RestaurantTable.TableNumber;
            existingTable.DiningArea = RestaurantTable.DiningArea;
            existingTable.Capacity = RestaurantTable.Capacity;
            existingTable.Status = RestaurantTable.Status;

            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}