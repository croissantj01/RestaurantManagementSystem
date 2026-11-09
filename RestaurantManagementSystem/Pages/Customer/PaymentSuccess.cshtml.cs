using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;
using System.Text;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class PaymentSuccessModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PaymentSuccessModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int OrderId { get; set; }

        public Order? Order { get; set; }
        public int PointsEarned { get; set; }

        public async Task<IActionResult> OnGetAsync(int? orderId)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            int targetOrderId = orderId ?? OrderId;

            Order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Menu)
                .FirstOrDefaultAsync(o => o.OrderId == targetOrderId && o.CustomerId == customerId.Value);

            if (Order == null)
            {
                return RedirectToPage("/Customer/OrderPortal");
            }

            // Mark paid, assign table status, award loyalty points
            if (Order.PaymentStatus != "Paid")
            {
                Order.PaymentStatus = "Paid";
                Order.Status = "Reserved";

                if (Order.Table != null)
                {
                    Order.Table.Status = "Occupied";
                }

                var customer = await _context.Customers.FindAsync(customerId.Value);
                if (customer != null)
                {
                    PointsEarned = (int)Math.Floor(Order.TotalAmount);
                    customer.LoyaltyPoints += PointsEarned;

                    await _context.AddNotificationAsync(customer.CustomerId,
                        $"Payment successful for Order #{Order.OrderId}! You earned {PointsEarned} loyalty points.");
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                PointsEarned = (int)Math.Floor(Order.TotalAmount);
            }

            ViewData["CustomerName"] = HttpContext.Session.GetString("CustomerName") ?? "Guest";
            return Page();
        }

        // Handler to stream clean printable HTML/PDF receipt
        public async Task<IActionResult> OnGetExportPdfAsync(int orderId)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Menu)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId.Value);

            if (order == null) return NotFound();

            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 30px; color: #333; }
                        .header { text-align: center; border-bottom: 2px dashed #ccc; padding-bottom: 15px; }
                        .title { font-size: 24px; font-weight: bold; margin: 0; }
                        .subtitle { font-size: 14px; color: #666; margin-top: 5px; }
                        .details { margin: 20px 0; font-size: 14px; }
                        .table { width: 100%; border-collapse: collapse; margin-top: 15px; }
                        .table th, .table td { border-bottom: 1px solid #ddd; padding: 10px; text-align: left; }
                        .table th { background-color: #f8f9fa; }
                        .total-row { font-size: 16px; font-weight: bold; }
                        .footer { margin-top: 30px; text-align: center; font-size: 12px; color: #777; }
                    </style>
                </head>
                <body onload='window.print()'>
                    <div class='header'>
                        <h1 class='title'>MyCafe Restaurant</h1>
                        <p class='subtitle'>Official Payment Receipt</p>
                    </div>
                    <div class='details'>
                        <p><strong>Order Reference:</strong> #ORD-" + order.OrderId.ToString("D5") + @"</p>
                        <p><strong>Date & Time:</strong> " + order.OrderDate.ToString("dd MMM yyyy, hh:mm tt") + @"</p>
                        <p><strong>Table Allocated:</strong> Table " + (order.Table?.TableNumber.ToString() ?? "N/A") + @"</p>
                        <p><strong>Payment Status:</strong> Paid (via Stripe)</p>
                    </div>
                    <table class='table'>
                        <thead>
                            <tr>
                                <th>Item</th>
                                <th>Size</th>
                                <th>Qty</th>
                                <th>Price</th>
                                <th>Subtotal</th>
                            </tr>
                        </thead>
                        <tbody>");

            foreach (var item in order.OrderItems)
            {
                decimal itemTotal = item.Price * item.Quantity;
                htmlBuilder.Append($@"
                    <tr>
                        <td>{(item.Menu != null ? item.Menu.MenuName : "Menu Item")}</td>
                        <td>{item.Size}</td>
                        <td>{item.Quantity}</td>
                        <td>RM {item.Price:N2}</td>
                        <td>RM {itemTotal:N2}</td>
                    </tr>");
            }

            htmlBuilder.Append($@"
                        </tbody>
                    </table>
                    <div style='margin-top: 20px; text-align: right;'>
                        <p class='total-row'>Total Paid: RM {order.TotalAmount:N2}</p>
                    </div>
                    <div class='footer'>
                        <p>Thank you for dining with MyCafe!</p>
                    </div>
                </body>
                </html>");

            byte[] pdfBytes = Encoding.UTF8.GetBytes(htmlBuilder.ToString());
            return File(pdfBytes, "text/html", $"Receipt_ORD_{order.OrderId:D5}.html");
        }
    }
}