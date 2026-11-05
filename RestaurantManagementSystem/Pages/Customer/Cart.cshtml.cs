using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class CartModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CartModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public SelectList? Tables { get; set; }
        public decimal Total { get; set; }
        public string? ErrorMessage { get; set; }

        [BindProperty]
        public int TableId { get; set; }

        [BindProperty]
        public DateTime DineInDateTime { get; set; } = DateTime.Now.AddHours(1);

        [BindProperty]
        public string? SpecialRequest { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToPage("/Customer/Login");
            }

            await LoadCartDataAsync(customerId.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveAsync(int id)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId.Value);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostPlaceOrderAsync()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            await LoadCartDataAsync(customerId.Value);

            if (!CartItems.Any())
            {
                ErrorMessage = "Your cart is empty. Please add items from the menu first.";
                return Page();
            }

            if (TableId <= 0)
            {
                ErrorMessage = "Please select a table for your dine-in order.";
                return Page();
            }

            var selectedTable = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.RestaurantTableId == TableId);

            if (selectedTable != null)
            {
                selectedTable.Status = "Occupied";
            }

            var order = new Order
            {
                CustomerId = customerId.Value,
                TableId = TableId,
                DineInDateTime = DineInDateTime,
                SpecialRequest = SpecialRequest,
                TotalAmount = Total,
                Status = "Pending",
                PaymentStatus = "Unpaid",
                OrderDate = DateTime.Now,
                OrderItems = CartItems.Select(c => new OrderItem
                {
                    MenuId = c.MenuId,
                    Quantity = c.Quantity,
                    Size = c.Size,
                    Price = c.Price,
                    Subtotal = c.Price * c.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(CartItems);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Customer/PaymentCheckout", new { orderId = order.OrderId });
        }

        private async Task LoadCartDataAsync(int customerId)
        {
            // Fetch cart items belonging specifically to this customer
            CartItems = await _context.CartItems
                .Where(c => c.CustomerId == customerId)
                .ToListAsync();

            Total = CartItems.Sum(c => c.Price * c.Quantity);

            var availableTables = await _context.RestaurantTables
                .Where(t => t.Status == "Available")
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            Tables = new SelectList(availableTables.Select(t => new
            {
                t.RestaurantTableId,
                DisplayText = $"Table {t.TableNumber} ({t.DiningArea} - Cap: {t.Capacity})"
            }), "RestaurantTableId", "DisplayText");
        }
    }
}