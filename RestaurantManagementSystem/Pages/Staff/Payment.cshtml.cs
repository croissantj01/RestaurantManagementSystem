using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Staff
{
    public class PaymentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PaymentsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Payment records
        public IList<Payment> Payments { get; set; }
            = new List<Payment>();

        // Summary card values
        public int TotalPayments { get; set; }
        public int PaidPayments { get; set; }
        public int PendingPayments { get; set; }
        public int FailedPayments { get; set; }

        // Search and filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchOrderId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedPaymentMethod { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        public async Task OnGetAsync()
        {
            // Get all payment records
            var query = _context.Payments
                .Include(p => p.Order)
                .AsQueryable();

            // Search by Order ID
            if (!string.IsNullOrWhiteSpace(SearchOrderId))
            {
                if (int.TryParse(SearchOrderId, out int orderId))
                {
                    query = query.Where(p => p.OrderId == orderId);
                }
                else
                {
                    query = query.Where(p => false);
                }
            }

            // Filter by payment status
            if (!string.IsNullOrWhiteSpace(SelectedStatus))
            {
                query = query.Where(p =>
                    p.Status == SelectedStatus);
            }

            // Filter by payment method
            if (!string.IsNullOrWhiteSpace(SelectedPaymentMethod))
            {
                query = query.Where(p =>
                    p.PaymentMethod == SelectedPaymentMethod);
            }

            // Filter by payment date
            if (SelectedDate.HasValue)
            {
                query = query.Where(p =>
                    p.PaymentDate.Date == SelectedDate.Value.Date);
            }

            // Retrieve filtered payment records
            Payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            // Calculate summary cards using ALL payment records
            TotalPayments = await _context.Payments.CountAsync();

            PaidPayments = await _context.Payments
                .CountAsync(p => p.Status == "Paid");

            PendingPayments = await _context.Payments
                .CountAsync(p => p.Status == "Pending");

            FailedPayments = await _context.Payments
                .CountAsync(p => p.Status == "Failed");
        }
    }
}


