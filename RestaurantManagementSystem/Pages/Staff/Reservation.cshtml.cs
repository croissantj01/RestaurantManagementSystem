using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Staff
{
    public class ReservationsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReservationsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Reservation> Reservations { get; set; } = new List<Reservation>();

        // Summary counts
        public int TotalReservations { get; set; }
        public int PendingReservations { get; set; }
        public int ConfirmedReservations { get; set; }
        public int CancelledReservations { get; set; }

        // Search and filter
        [BindProperty(SupportsGet = true)]
        public string? SearchCustomer { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        public async Task OnGetAsync()
        {
            // Get summary counts from database
            TotalReservations = await _context.Reservations.CountAsync();

            PendingReservations = await _context.Reservations
                .CountAsync(r => r.Status == "Reserved" || r.Status == "Pending");

            ConfirmedReservations = await _context.Reservations
                .CountAsync(r => r.Status == "Confirmed");

            CancelledReservations = await _context.Reservations
                .CountAsync(r => r.Status == "Cancelled");

            // Retrieve reservations
            var query = _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.RestaurantTable)
                .AsQueryable();

            // Search by customer name
            if (!string.IsNullOrWhiteSpace(SearchCustomer))
            {
                query = query.Where(r =>
                    r.Customer.FullName.Contains(SearchCustomer));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(SelectedStatus))
            {
                if (SelectedStatus == "Pending")
                {
                    query = query.Where(r =>
                        r.Status == "Pending" || r.Status == "Reserved");
                }
                else
                {
                    query = query.Where(r => r.Status == SelectedStatus);
                }
            }

            // Filter by reservation date
            if (SelectedDate.HasValue)
            {
                query = query.Where(r =>
                    r.ReservationDate.Date == SelectedDate.Value.Date);
            }

            Reservations = await query
                .OrderByDescending(r => r.ReservationDate)
                .ThenBy(r => r.ReservationTime)
                .ToListAsync();
        }

        // Confirm reservation
        public async Task<IActionResult> OnPostConfirmAsync(int id)
        {
            var reservation = await _context.Reservations
                .FindAsync(id);

            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.Status == "Reserved" ||
                reservation.Status == "Pending")
            {
                reservation.Status = "Confirmed";
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        // Cancel reservation
        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            var reservation = await _context.Reservations
                .FindAsync(id);

            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.Status != "Cancelled")
            {
                reservation.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}

