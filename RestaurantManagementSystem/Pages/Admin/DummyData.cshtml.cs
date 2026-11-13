using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;

namespace RestaurantManagementSystem.Pages.Admin
{
    // Temporary admin page: add / refresh / remove dummy data for testing reports.
    // Delete this page and Data/DbSeeder.cs before you submit.
    public class DummyDataModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DummyDataModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int MenuCount { get; set; }
        public int StaffCount { get; set; }
        public int CustomerCount { get; set; }
        public int OrderCount { get; set; }
        public int ReservationCount { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        private bool IsAdmin =>
            HttpContext.Session.GetString("AdminLoggedIn") == "true";

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAdmin) return RedirectToPage("/Index", new { login = "true" });

            await LoadCountsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSeedAsync()
        {
            if (!IsAdmin) return RedirectToPage("/Index", new { login = "true" });

            try
            {
                StatusMessage = await DbSeeder.SeedAsync(_context);
            }
            catch (DbUpdateException ex)
            {
                ErrorMessage = "Could not add dummy data: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAsync()
        {
            if (!IsAdmin) return RedirectToPage("/Index", new { login = "true" });

            try
            {
                await DbSeeder.RemoveAsync(_context);
                StatusMessage = "All dummy data has been removed.";
            }
            catch (DbUpdateException ex)
            {
                ErrorMessage = "Could not remove all dummy data. Some of it is used by other records: "
                    + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage();
        }

        private async Task LoadCountsAsync()
        {
            var counts = await DbSeeder.GetCountsAsync(_context);

            MenuCount = counts.Menus;
            StaffCount = counts.Staff;
            CustomerCount = counts.Customers;
            OrderCount = counts.Orders;
            ReservationCount = counts.Reservations;
        }
    }
}
