using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class MyReservationModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MyReservationModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // List of reservations belonging to the logged-in customer
        public List<Reservation> Reservations { get; set; } = new List<Reservation>();

        // GET: My Reservations
        public async Task<IActionResult> OnGetAsync()
        {
            // Check whether customer is logged in
            int? customerId = HttpContext.Session.GetInt32("CustomerId");

            // If not logged in, go to Login page
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            // Get only reservations belonging to
            // the currently logged-in customer
            Reservations = await _context.Reservations
                .Include(r => r.RestaurantTable)
                .Where(r => r.CustomerId == customerId.Value)
                .OrderByDescending(r => r.ReservationDate)
                .ThenByDescending(r => r.ReservationTime)
                .ToListAsync();

            return Page();
        }

        // CANCEL RESERVATION
        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            // Check whether customer is logged in
            int? customerId =
                HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            // Find the reservation belonging to
            // the logged-in customer
            var reservation =
                await _context.Reservations
                    .FirstOrDefaultAsync(r =>
                        r.ReservationId == id &&
                        r.CustomerId == customerId.Value);

            // Reservation does not exist
            if (reservation == null)
            {
                return RedirectToPage();
            }

            // Only Reserved reservations can be cancelled
            if (reservation.Status == "Reserved")
            {
                reservation.Status = "Cancelled";

                await _context.SaveChangesAsync();
            }

            // Reload My Reservations page
            return RedirectToPage();
        }
    }
}