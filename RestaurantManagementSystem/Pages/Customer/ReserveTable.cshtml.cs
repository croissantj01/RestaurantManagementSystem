using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class ReserveTableModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReserveTableModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // AVAILABLE TABLES
        public List<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();

        // SELECTED TABLE
        [BindProperty]
        public int SelectedTableId { get; set; }

        // DINE-IN DATE
        [BindProperty]
        public DateTime ReservationDate { get; set; }

        // DINE-IN TIME
        [BindProperty]
        public TimeSpan ReservationTime { get; set; }

        // NUMBER OF GUESTS
        [BindProperty]
        public int NumberOfGuests { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check customer login
            int? customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            // Default date = today
            ReservationDate = DateTime.Today;

            // Default time = 7:00 PM
            ReservationTime =
                new TimeSpan(19, 0, 0);

            // Default guests
            NumberOfGuests = 2;

            // Load tables available at
            // the selected date and time
            await LoadAvailableTables();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Check customer login
            int? customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            if (ReservationDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "ReservationDate",
                    "Please select today or a future date."
                );
            }

            if (ReservationTime < TimeSpan.Zero || ReservationTime >= new TimeSpan(24, 0, 0))
            {
                ModelState.AddModelError(
                    "ReservationTime",
                    "Please select a valid reservation time."
                );
            }

            if (SelectedTableId <= 0)
            {
                ModelState.AddModelError(
                    "SelectedTableId",
                    "Please select a table."
                );
            }

            if (NumberOfGuests <= 0)
            {
                ModelState.AddModelError(
                    "NumberOfGuests",
                    "Please enter the number of guests."
                );
            }

            RestaurantTable? selectedTable = null;
            if (SelectedTableId > 0)
            {
                selectedTable = await _context.RestaurantTables
                    .FirstOrDefaultAsync(t => t.RestaurantTableId == SelectedTableId);
            }

            if (selectedTable == null)
            {
                ModelState.AddModelError(
                    "SelectedTableId",
                    "The selected table does not exist."
                );
            }
            else
            {
                // Check normal table status
                if (selectedTable.Status != "Available")
                {
                    ModelState.AddModelError(
                        "SelectedTableId",
                        "This table is currently unavailable."
                    );
                }

                // Check capacity
                if (NumberOfGuests > selectedTable.Capacity)
                {
                    ModelState.AddModelError(
                        "NumberOfGuests",
                        $"This table can accommodate a maximum of {selectedTable.Capacity} guests."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadAvailableTables();
                return Page();
            }

            // Reservation lasts exactly 2 hours
            TimeSpan newStart = ReservationTime;

            TimeSpan newEnd = ReservationTime.Add(
                TimeSpan.FromHours(2));

            var existingReservations = await _context.Reservations
                .Where(r =>
                    r.RestaurantTableId == SelectedTableId &&
                    r.ReservationDate.Date == ReservationDate.Date &&
                    r.Status == "Reserved")
                .ToListAsync();

            bool hasOverlap = false;

            foreach (var existing in existingReservations)
            {
                TimeSpan existingStart = existing.ReservationTime;

                TimeSpan existingEnd = existingStart.Add(
                    TimeSpan.FromHours(2));

                // Check whether the two
                // 2-hour periods overlap
                if (newStart < existingEnd &&
                    newEnd > existingStart)
                {
                    hasOverlap = true;
                    break;
                }
            }

            if (hasOverlap)
            {
                ModelState.AddModelError(
                    "",
                    "This table is already reserved for the selected date and time. Please select another table."
                );
                await LoadAvailableTables();
                return Page();
            }

            var reservation = new Reservation
            {
                CustomerId = customerId.Value,
                RestaurantTableId = SelectedTableId,
                // IMPORTANT:
                // Save the selected date
                ReservationDate = ReservationDate.Date,
                // Save the selected time
                ReservationTime = ReservationTime,
                NumberOfGuests = NumberOfGuests,
                Status = "Reserved"
            };

            // Add reservation
            _context.Reservations.Add(reservation);

            // Save
            await _context.SaveChangesAsync();

            // Success message
            TempData["SuccessMessage"] =
                "Table reserved successfully for 2 hours.";

            // Go to My Reservation
            return RedirectToPage("/Customer/MyReservation");
        }

        private async Task LoadAvailableTables()
        {
            // Get all tables that are normally available
            var allTables = await _context.RestaurantTables
                .Where(t =>
                    t.Status == "Available")
                .OrderBy(t =>
                    t.TableNumber)
                .ToListAsync();

            // Reservation requested by customer
            TimeSpan requestedStart = ReservationTime;

            TimeSpan requestedEnd =
                ReservationTime.Add(
                    TimeSpan.FromHours(2));

            // Get reservations for the selected date
            var reservations = await _context.Reservations
                .Where(r =>
                    r.ReservationDate.Date ==
                    ReservationDate.Date &&
                    r.Status == "Reserved")
                .ToListAsync();

            // Remove tables that have
            // overlapping reservations
            Tables =
                allTables
                    .Where(table =>
                    {
                        var tableReservations =
                            reservations
                                .Where(r =>
                                    r.RestaurantTableId ==
                                    table.RestaurantTableId);

                        foreach (var reservation in tableReservations)
                        {
                            TimeSpan existingStart =
                                reservation.ReservationTime;

                            TimeSpan existingEnd =
                                existingStart.Add(
                                    TimeSpan.FromHours(2));

                            // Overlap check
                            if (requestedStart < existingEnd &&
                                requestedEnd > existingStart)
                            {
                                return false;
                            }
                        }

                        return true;
                    })
                    .ToList();
        }
    }
}