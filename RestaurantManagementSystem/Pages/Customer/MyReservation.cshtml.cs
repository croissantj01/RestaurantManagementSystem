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

        public List<ReservationViewModel> Bookings { get; set; } = new List<ReservationViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            Bookings = new List<ReservationViewModel>();

            // 1. Fetch standard Table Reservations (Table-Only)
            var reservations = await _context.Reservations
                .Include(r => r.RestaurantTable)
                .Where(r => r.CustomerId == customerId.Value)
                .ToListAsync();

            foreach (var r in reservations)
            {
                Bookings.Add(new ReservationViewModel
                {
                    Id = r.ReservationId,
                    Type = "Table Only",
                    TableNumber = r.RestaurantTable != null ? r.RestaurantTable.TableNumber.ToString() : "N/A",
                    DiningArea = r.RestaurantTable?.DiningArea ?? "N/A",
                    Date = r.ReservationDate,
                    StartTime = r.ReservationDate.Date.Add(r.ReservationTime),
                    Guests = r.NumberOfGuests,
                    Status = r.Status,
                    OrderItems = new List<OrderItem>()
                });
            }

            // 2. Fetch Dine-In Food Orders (Table + Food)
            var orders = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Menu)
                .Where(o => o.CustomerId == customerId.Value && o.TableId != null)
                .ToListAsync();

            foreach (var o in orders)
            {
                Bookings.Add(new ReservationViewModel
                {
                    Id = o.OrderId,
                    Type = "Table + Food Order",
                    TableNumber = o.Table != null ? o.Table.TableNumber.ToString() : "N/A",
                    DiningArea = o.Table?.DiningArea ?? "N/A",
                    Date = o.DineInDateTime.Date,
                    StartTime = o.DineInDateTime,
                    Guests = 2,
                    Status = (o.Status == "Completed" || o.PaymentStatus == "Paid") ? "Reserved" : o.Status,
                    TotalAmount = o.TotalAmount,
                    OrderItems = o.OrderItems.ToList()
                });
            }

            Bookings = Bookings.OrderByDescending(b => b.Date).ThenByDescending(b => b.StartTime).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(int id, string type)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            if (type == "Table Only")
            {
                var reservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.ReservationId == id && r.CustomerId == customerId.Value);

                if (reservation != null && reservation.Status == "Reserved")
                {
                    reservation.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                }
            }
            else if (type == "Table + Food Order")
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == customerId.Value);

                if (order != null && (order.Status == "Pending" || order.Status == "Completed"))
                {
                    order.Status = "Cancelled";
                    if (order.TableId != null)
                    {
                        var table = await _context.RestaurantTables.FindAsync(order.TableId);
                        if (table != null) table.Status = "Available";
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage();
        }
    }

    public class ReservationViewModel
    {
        public int Id { get; set; }
        public string Type { get; set; } = "Table Only";
        public string TableNumber { get; set; } = "";
        public string DiningArea { get; set; } = "";
        public DateTime Date { get; set; }
        public DateTime StartTime { get; set; }
        public int Guests { get; set; }
        public string Status { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}