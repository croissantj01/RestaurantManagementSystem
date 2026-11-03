using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;

namespace RestaurantManagementSystem.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Statuses that are not counted in the "today" cards
        // (both spellings are covered)
        private static readonly string[] CancelledStatuses =
            { "Cancelled", "Canceled" };

        // Numbers shown in the overview cards
        public int TodayOrders { get; set; }
        public int TodayReservations { get; set; }
        public int ReservedTables { get; set; }
        public int TotalTables { get; set; }
        public int CustomerCount { get; set; }
        public int StaffCount { get; set; }
        public int MenuCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check whether admin is logged in
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Index", new { login = "true" });
            }

            // "Today" runs from midnight up to (not including) tomorrow's midnight
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            // Today's orders: Pending + Preparing + Served, not Cancelled
            TodayOrders = await _context.Orders
                .CountAsync(o =>
                    o.DineInDateTime >= today &&
                    o.DineInDateTime < tomorrow &&
                    !CancelledStatuses.Contains(o.Status));

            // Today's reservations, not Cancelled
            TodayReservations = await _context.Reservations
                .CountAsync(r =>
                    r.ReservationDate >= today &&
                    r.ReservationDate < tomorrow &&
                    !CancelledStatuses.Contains(r.Status));

            // Reserved tables: different tables booked today (a table booked
            // twice is counted once), not Cancelled
            ReservedTables = await _context.Reservations
                .Where(r =>
                    r.ReservationDate >= today &&
                    r.ReservationDate < tomorrow &&
                    !CancelledStatuses.Contains(r.Status))
                .Select(r => r.RestaurantTableId)
                .Distinct()
                .CountAsync();

            // All tables in the restaurant (the "25" in "3/25")
            TotalTables = await _context.RestaurantTables.CountAsync();

            CustomerCount = await _context.Customers.CountAsync();
            StaffCount = await _context.Staffs.CountAsync();
            MenuCount = await _context.Menus.CountAsync();

            return Page();
        }

        public IActionResult OnPostLogout()
        {
            // Clear admin session
            HttpContext.Session.Clear();

            // Back to the homepage
            return RedirectToPage("/Index");
        }
    }
}