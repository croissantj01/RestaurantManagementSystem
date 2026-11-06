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
        public decimal DiscountAmount { get; set; }
        public string DiscountDescription { get; set; } = "";
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        public string? AppliedPromoCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public int SelectedPax { get; set; } = 1;

        [BindProperty]
        public int TableId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? pax, string? code)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            if (pax.HasValue && pax.Value > 0)
            {
                SelectedPax = pax.Value;
            }

            if (!string.IsNullOrWhiteSpace(code))
            {
                string cleanCode = code.Trim().ToUpper();

                var voucher = await _context.RedeemedVouchers
                    .FirstOrDefaultAsync(v => v.PromoCode == cleanCode && v.CustomerId == customerId.Value && !v.IsUsed);

                if (voucher != null)
                {
                    HttpContext.Session.SetString("ActivePromoCode", cleanCode);
                    TempData["SuccessMessage"] = $"Promo code '{cleanCode}' applied automatically!";
                }
            }

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
            }

            return RedirectToPage(new { pax = SelectedPax });
        }

        public async Task<IActionResult> OnPostRemoveCartItemAsync(int cartItemId)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.CustomerId == customerId.Value);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { pax = SelectedPax });
        }

        public async Task<IActionResult> OnPostApplyPromoCodeAsync(string promoCode)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            if (string.IsNullOrWhiteSpace(promoCode))
            {
                TempData["ErrorMessage"] = "Please enter a valid promo code.";
                return RedirectToPage(new { pax = SelectedPax });
            }

            string cleanCode = promoCode.Trim().ToUpper();

            var voucher = await _context.RedeemedVouchers
                .FirstOrDefaultAsync(v => v.PromoCode == cleanCode && v.CustomerId == customerId.Value);

            if (voucher == null)
            {
                TempData["ErrorMessage"] = "Invalid promo code or this code does not belong to your account.";
                return RedirectToPage(new { pax = SelectedPax });
            }

            if (voucher.IsUsed)
            {
                TempData["ErrorMessage"] = "This promo code has already been used on a previous order.";
                return RedirectToPage(new { pax = SelectedPax });
            }

            HttpContext.Session.SetString("ActivePromoCode", cleanCode);
            TempData["SuccessMessage"] = $"Promo code '{cleanCode}' applied successfully!";

            return RedirectToPage(new { pax = SelectedPax });
        }

        public async Task<IActionResult> OnPostRemovePromoCodeAsync()
        {
            HttpContext.Session.Remove("ActivePromoCode");
            TempData["SuccessMessage"] = "Promo code removed.";
            return RedirectToPage(new { pax = SelectedPax });
        }

        public async Task<IActionResult> OnPostConfirmOrderAsync()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToPage("/Customer/Login");

            var cart = await _context.CartItems.Where(c => c.CustomerId == customerId.Value).ToListAsync();
            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add items to proceed.";
                return RedirectToPage(new { pax = SelectedPax });
            }

            if (TableId <= 0)
            {
                TempData["ErrorMessage"] = "Please select a valid dine-in table.";
                return RedirectToPage(new { pax = SelectedPax });
            }

            await LoadDataAsync(customerId.Value);

            string? activeCode = HttpContext.Session.GetString("ActivePromoCode");
            if (!string.IsNullOrEmpty(activeCode))
            {
                var voucher = await _context.RedeemedVouchers
                    .FirstOrDefaultAsync(v => v.PromoCode == activeCode && v.CustomerId == customerId.Value && !v.IsUsed);

                if (voucher != null)
                {
                    voucher.IsUsed = true;
                    _context.RedeemedVouchers.Update(voucher);
                }
            }

            HttpContext.Session.SetInt32("SelectedTableId", TableId);

            var newOrder = new Order
            {
                CustomerId = customerId.Value,
                TableId = TableId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                PaymentStatus = "Unpaid",
                TotalAmount = Total,
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

            HttpContext.Session.Remove("ActivePromoCode");

            await _context.SaveChangesAsync();

            await _context.AddNotificationAsync(customerId.Value, $"Order #{newOrder.OrderId} created successfully!");

            return RedirectToPage("/Customer/PaymentCheckout", new { orderId = newOrder.OrderId });
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

            AppliedPromoCode = HttpContext.Session.GetString("ActivePromoCode");
            DiscountAmount = 0m;
            DiscountDescription = "";

            if (!string.IsNullOrEmpty(AppliedPromoCode) && CartItems.Any())
            {
                var voucherCheck = await _context.RedeemedVouchers
                    .FirstOrDefaultAsync(v => v.PromoCode == AppliedPromoCode && v.CustomerId == customerId && !v.IsUsed);

                if (voucherCheck == null)
                {
                    HttpContext.Session.Remove("ActivePromoCode");
                    AppliedPromoCode = null;
                }
                else if (AppliedPromoCode.StartsWith("DRINK-"))
                {
                    var drinkItem = CartItems.FirstOrDefault(c =>
                    {
                        var menu = Menus.FirstOrDefault(m => m.Id == c.MenuId);
                        return menu != null && !string.IsNullOrEmpty(menu.Category) &&
                               (menu.Category.Contains("Beverage", StringComparison.OrdinalIgnoreCase) ||
                                menu.Category.Contains("Drink", StringComparison.OrdinalIgnoreCase));
                    });

                    if (drinkItem != null)
                    {
                        DiscountAmount = drinkItem.Price;
                        DiscountDescription = "Free Drink";
                    }
                    else
                    {
                        DiscountDescription = "Add a drink to use code";
                    }
                }
                else if (AppliedPromoCode.StartsWith("DESSERT-"))
                {
                    var dessertItem = CartItems.FirstOrDefault(c =>
                    {
                        var menu = Menus.FirstOrDefault(m => m.Id == c.MenuId);
                        return menu != null && !string.IsNullOrEmpty(menu.Category) &&
                               menu.Category.Contains("Dessert", StringComparison.OrdinalIgnoreCase);
                    });

                    if (dessertItem != null)
                    {
                        DiscountAmount = dessertItem.Price;
                        DiscountDescription = "Free Dessert";
                    }
                    else
                    {
                        DiscountDescription = "Add a dessert to use code";
                    }
                }
                else if (AppliedPromoCode.StartsWith("VOUCHER10-"))
                {
                    DiscountAmount = 10.00m;
                    DiscountDescription = "RM10 Off Voucher";
                }
            }

            decimal discountedSubtotal = Math.Max(0m, Subtotal - DiscountAmount);
            Tax = discountedSubtotal * 0.06m;
            Total = discountedSubtotal + Tax;

            var tables = await _context.RestaurantTables
                .Where(t => t.Status == "Available" && t.Capacity >= SelectedPax)
                .OrderBy(t => t.Capacity)
                .ThenBy(t => t.TableNumber)
                .ToListAsync();

            Tables = new SelectList(tables.Select(t => new
            {
                t.RestaurantTableId,
                DisplayText = $"Table {t.TableNumber} ({t.DiningArea} - {t.Capacity} Seats)"
            }), "RestaurantTableId", "DisplayText");
        }
    }
}