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

            // Load this customer's existing reviews once
            var myReviews = await _context.Reviews
                .Where(rv => rv.CustomerId == customerId.Value)
                .ToListAsync();

            // 1. Fetch standard Table Reservations (Table-Only)
            var reservations = await _context.Reservations
                .Include(r => r.RestaurantTable)
                .Where(r => r.CustomerId == customerId.Value && r.Status != "Hidden")
                .ToListAsync();

            foreach (var r in reservations)
            {
                if (r.Status == "Hidden") continue;

                DateTime reservationDateTime = (r.ReservationDate != default && r.ReservationDate.Year > 1)
                    ? r.ReservationDate.Date.Add(r.ReservationTime)
                    : (r.CreatedAt != default && r.CreatedAt.Year > 1 ? r.CreatedAt : DateTime.Now);

                var review = myReviews.FirstOrDefault(rv => rv.BookingType == "Table Only" && rv.BookingId == r.ReservationId);

                Bookings.Add(new ReservationViewModel
                {
                    Id = r.ReservationId,
                    Type = "Table Only",
                    TableNumber = r.RestaurantTable != null ? r.RestaurantTable.TableNumber.ToString() : "N/A",
                    DiningArea = r.RestaurantTable?.DiningArea ?? "N/A",
                    Date = reservationDateTime.Date,
                    StartTime = reservationDateTime,
                    Guests = r.NumberOfGuests,
                    Status = r.Status,
                    IsCompleted = r.Status == "Completed",
                    ReviewRating = review?.Rating,
                    ReviewComment = review?.Comment,
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
                if (o.Status == "Hidden") continue;

                DateTime orderDateTime = (o.DineInDateTime != default && o.DineInDateTime.Year > 1)
                    ? o.DineInDateTime
                    : (o.OrderDate != default && o.OrderDate.Year > 1 ? o.OrderDate : DateTime.Now);

                string displayStatus = o.Status;
                if (o.Status != "Cancelled")
                {
                    displayStatus = (o.Status == "Completed" || o.PaymentStatus == "Paid") ? "Reserved" : o.Status;
                }

                var review = myReviews.FirstOrDefault(rv => rv.BookingType == "Table + Food Order" && rv.BookingId == o.OrderId);

                Bookings.Add(new ReservationViewModel
                {
                    Id = o.OrderId,
                    Type = "Table + Food Order",
                    TableNumber = o.Table != null ? o.Table.TableNumber.ToString() : "N/A",
                    DiningArea = o.Table?.DiningArea ?? "N/A",
                    Date = orderDateTime.Date,
                    StartTime = orderDateTime,
                    Guests = 2,
                    Status = displayStatus,
                    IsCompleted = o.Status == "Completed",
                    ReviewRating = review?.Rating,
                    ReviewComment = review?.Comment,
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

                if (reservation != null && reservation.Status != "Cancelled")
                {
                    reservation.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                }
            }
            else if (type == "Table + Food Order")
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == customerId.Value);

                if (order != null && order.Status != "Cancelled")
                {
                    order.Status = "Cancelled";

                    if (order.TableId != null)
                    {
                        var table = await _context.RestaurantTables.FindAsync(order.TableId);
                        if (table != null)
                        {
                            table.Status = "Available";
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage();
        }

        // Handler to hide cancelled bookings using the trash can button
        public async Task<IActionResult> OnPostHideAsync(int id, string type)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            if (type == "Table Only")
            {
                var reservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.ReservationId == id && r.CustomerId == customerId.Value);

                if (reservation != null)
                {
                    reservation.Status = "Hidden";
                    await _context.SaveChangesAsync();
                }
            }
            else if (type == "Table + Food Order")
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == customerId.Value);

                if (order != null)
                {
                    order.Status = "Hidden";
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage();
        }

        // Handler to submit a review for a completed booking
        public async Task<IActionResult> OnPostReviewAsync(int id, string type, int rating, string? comment)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            // Validate rating
            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Please select a star rating from 1 to 5.";
                return RedirectToPage();
            }

            // Validate comment (max 100 words)
            comment = comment?.Trim();
            if (!string.IsNullOrEmpty(comment))
            {
                int wordCount = comment.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
                if (wordCount > 100 || comment.Length > 1000)
                {
                    TempData["ErrorMessage"] = "Your review is too long. Please keep it within 100 words.";
                    return RedirectToPage();
                }
            }
            else
            {
                comment = null;
            }

            // Make sure the booking belongs to this customer and is completed
            bool isCompleted = false;

            if (type == "Table Only")
            {
                isCompleted = await _context.Reservations
                    .AnyAsync(r => r.ReservationId == id && r.CustomerId == customerId.Value && r.Status == "Completed");
            }
            else if (type == "Table + Food Order")
            {
                isCompleted = await _context.Orders
                    .AnyAsync(o => o.OrderId == id && o.CustomerId == customerId.Value && o.Status == "Completed");
            }

            if (!isCompleted)
            {
                TempData["ErrorMessage"] = "You can only review a completed booking.";
                return RedirectToPage();
            }

            // One review per booking
            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(rv => rv.CustomerId == customerId.Value && rv.BookingType == type && rv.BookingId == id);

            if (alreadyReviewed)
            {
                TempData["ErrorMessage"] = "You have already reviewed this booking.";
                return RedirectToPage();
            }

            _context.Reviews.Add(new Review
            {
                CustomerId = customerId.Value,
                BookingType = type,
                BookingId = id,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you! Your review has been submitted.";
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
        public bool IsCompleted { get; set; }
        public int? ReviewRating { get; set; }
        public string? ReviewComment { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}