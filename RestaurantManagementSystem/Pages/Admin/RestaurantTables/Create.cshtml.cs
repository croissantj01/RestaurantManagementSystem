using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Admin.RestaurantTables
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public RestaurantTable RestaurantTable { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check for duplicate table number
            bool tableExists = await _context.RestaurantTables
                .AnyAsync(t =>
                    t.TableNumber == RestaurantTable.TableNumber);

            if (tableExists)
            {
                ModelState.AddModelError(
                    "RestaurantTable.TableNumber",
                    "This table number already exists."
                );

                return Page();
            }

            // Add new restaurant table
            _context.RestaurantTables.Add(RestaurantTable);

            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}