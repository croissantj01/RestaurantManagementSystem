using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class OrderPortalModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public OrderPortalModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Models.Menu> Menus { get; set; } = new List<Models.Menu>();
        public List<Models.Menu> PopularMenus { get; set; } = new List<Models.Menu>();
        public List<string> Categories { get; set; } = new List<string>();
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public SelectList? Tables { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        [BindProperty]
        public int TableId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            // Fixed: Using customer.FullName instead of customer.Name
            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer != null)
            {
                ViewData["CustomerName"] = customer.FullName;
            }

            await LoadDataAsync(customerId.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostAddToCartAsync(int menuId, string? size, int quantity)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == menuId);
            if (menu != null)
            {
                string selectedSize = "Regular";
                if (!string.IsNullOrWhiteSpace(size))
                {
                    if (size.Equals("L", StringComparison.OrdinalIgnoreCase) || size.Equals("Large", StringComparison.OrdinalIgnoreCase))
                    {
                        selectedSize = "Large";
                    }
                }

                int itemQty = quantity > 0 ? quantity : 1;
                decimal price = menu.Price + (selectedSize.Equals("Large", StringComparison.OrdinalIgnoreCase) ? 2.00m : 0m);

                var existing = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value && c.MenuId == menuId && c.Size == selectedSize);

                if (existing != null)
                {
                    existing.Quantity += itemQty;
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        CustomerId = customerId.Value,
                        MenuId = menu.Id,
                        MenuName = menu.MenuName,
                        Price = price,
                        Size = selectedSize,
                        Quantity = itemQty
                    });
                }

                await _context.SaveChangesAsync();

                // Trigger notification after item is saved
                await _context.AddNotificationAsync(customerId.Value, $"Added {menu.MenuName} (x{itemQty}) to your cart.");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveCartItemAsync(int cartItemId)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.CustomerId == customerId.Value);
            if (item != null)
            {
                string itemName = item.MenuName;
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();

                // Trigger notification after item is removed
                await _context.AddNotificationAsync(customerId.Value, $"Removed {itemName} from your cart.");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostConfirmOrderAsync()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var cart = await _context.CartItems.Where(c => c.CustomerId == customerId.Value).ToListAsync();
            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add items to proceed.";
                return RedirectToPage();
            }

            if (TableId <= 0)
            {
                TempData["ErrorMessage"] = "Please select a dine-in table.";
                return RedirectToPage();
            }

            HttpContext.Session.SetInt32("SelectedTableId", TableId);

            var newOrder = new Order
            {
                CustomerId = customerId.Value,
                TableId = TableId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                PaymentStatus = "Unpaid",
                TotalAmount = cart.Sum(c => c.Price * c.Quantity) + (cart.Sum(c => c.Price * c.Quantity) * 0.06m),
                OrderItems = cart.Select(c => new OrderItem
                {
                    MenuId = c.MenuId,
                    Quantity = c.Quantity,
                    Price = c.Price,
                    Size = c.Size
                }).ToList()
            };

            _context.Orders.Add(newOrder);
            _context.CartItems.RemoveRange(cart);
            await _context.SaveChangesAsync();

            // Trigger notification for order placement
            await _context.AddNotificationAsync(customerId.Value, $"Order #{newOrder.OrderId} created successfully! Please proceed to payment.");

            return RedirectToPage("/Customer/PaymentCheckout", new { orderId = newOrder.OrderId });
        }

        // Handler to mark individual notifications as read when clicked in the bell dropdown
        public async Task<IActionResult> OnPostMarkSingleNotificationAsync(int id)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != null)
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.CustomerId == customerId.Value);

                if (notification != null)
                {
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();
                }
            }
            return new JsonResult(new { success = true });
        }

        private async Task LoadDataAsync(int customerId)
        {
            Menus = await _context.Menus.OrderBy(m => m.MenuName).ToListAsync();
            CartItems = await _context.CartItems.Where(c => c.CustomerId == customerId).ToListAsync();

            Categories = Menus
                .Where(m => !string.IsNullOrWhiteSpace(m.Category))
                .Select(m => m.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            if (!Categories.Any())
            {
                Categories = new List<string> { "Main Course", "Appetizers", "Beverages", "Desserts" };
            }

            var topMenuIds = await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .SelectMany(o => o.OrderItems)
                .GroupBy(oi => oi.MenuId)
                .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                .Select(g => g.Key)
                .Take(4)
                .ToListAsync();

            PopularMenus = Menus.Where(m => topMenuIds.Contains(m.Id)).ToList();

            Subtotal = CartItems.Sum(c => c.Price * c.Quantity);
            Tax = Subtotal * 0.06m;
            Total = Subtotal + Tax;

            var tables = await _context.RestaurantTables.Where(t => t.Status == "Available").OrderBy(t => t.TableNumber).ToListAsync();
            Tables = new SelectList(tables.Select(t => new
            {
                t.RestaurantTableId,
                DisplayText = $"Table {t.TableNumber} ({t.DiningArea})"
            }), "RestaurantTableId", "DisplayText");
        }
    }
}