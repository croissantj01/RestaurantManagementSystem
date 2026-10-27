using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Models;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Orders
{
    public class MakeOrderModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MakeOrderModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // MENU LIST
        public IList<RestaurantManagementSystem.Models.Menu> Menus { get; set; }
            = new List<RestaurantManagementSystem.Models.Menu>();

        // AVAILABLE TABLE LIST
        public IList<RestaurantTable> Tables { get; set; }
            = new List<RestaurantTable>();

        // ORDER
        [BindProperty]
        public Order Order { get; set; } = new Order();

        // MENU QUANTITIES
        [BindProperty]
        public Dictionary<int, int> Quantities { get; set; }
            = new Dictionary<int, int>();

        // DINE-IN DATE
        [BindProperty]
        public DateTime DineInDate { get; set; }

        // SELECTED HOUR
        [BindProperty]
        public int SelectedHour { get; set; }

        // SELECTED MINUTE
        [BindProperty]
        public int SelectedMinute { get; set; }

        // AM / PM
        [BindProperty]
        public string SelectedAmPm { get; set; } = "AM";

        // GET
        public async Task OnGetAsync()
        {
            // Load all menu items
            Menus = await _context.Menus
                .OrderBy(m => m.MenuName)
                .ToListAsync();

            // Load ONLY tables that are Available
            Tables = await _context.RestaurantTables
                .Where(t => t.Status == "Available")
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            // DEFAULT DINE-IN DATE AND TIME

            // Default time = one hour from now
            DateTime defaultDateTime = DateTime.Now.AddHours(1);

            DineInDate = defaultDateTime.Date;

            // CONVERT TO 12-HOUR FORMAT
            if (defaultDateTime.Hour == 0)
            {
                // 12:xx AM
                SelectedHour = 12;
                SelectedAmPm = "AM";
            }
            else if (defaultDateTime.Hour < 12)
            {
                // 1:xx AM - 11:xx AM
                SelectedHour = defaultDateTime.Hour;
                SelectedAmPm = "AM";
            }
            else if (defaultDateTime.Hour == 12)
            {
                // 12:xx PM
                SelectedHour = 12;
                SelectedAmPm = "PM";
            }
            else
            {
                // 1:xx PM - 11:xx PM
                SelectedHour = defaultDateTime.Hour - 12;
                SelectedAmPm = "PM";
            }

            SelectedMinute = defaultDateTime.Minute;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // RELOAD MENU ITEMS
            Menus = await _context.Menus
                .OrderBy(m => m.MenuName)
                .ToListAsync();

            // RELOAD AVAILABLE TABLES
            Tables = await _context.RestaurantTables
                .Where(t => t.Status == "Available")
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            // CONVERT AM / PM TO 24-HOUR FORMAT
            int hour24;

            if (SelectedAmPm == "AM")
            {
                if (SelectedHour == 12)
                {
                    // 12 AM = 00:00
                    hour24 = 0;
                }
                else
                {
                    hour24 = SelectedHour;
                }
            }
            else
            {
                if (SelectedHour == 12)
                {
                    // 12 PM = 12:00
                    hour24 = 12;
                }
                else
                {
                    // 1 PM = 13:00
                    // 2 PM = 14:00
                    // etc.
                    hour24 = SelectedHour + 12;
                }
            }

            // COMBINE DATE AND TIME
            Order.DineInDateTime = DineInDate.Date
                .AddHours(hour24)
                .AddMinutes(SelectedMinute);

            // CHECK FUTURE DATE AND TIME
            if (Order.DineInDateTime <= DateTime.Now)
            {
                ModelState.AddModelError(
                    "",
                    "Please select a future dine-in date and time."
                );
            }

            // CHECK TABLE SELECTION
            if (Order.TableId <= 0)
            {
                ModelState.AddModelError(
                    "Order.TableId",
                    "Please select a table."
                );
            }

            // CHECK TABLE AVAILABILITY
            RestaurantTable? selectedTable = null;

            if (Order.TableId > 0)
            {
                selectedTable = await _context.RestaurantTables
                    .FirstOrDefaultAsync(t => t.RestaurantTableId == Order.TableId);

                // Table does not exist
                if (selectedTable == null)
                {
                    ModelState.AddModelError(
                        "Order.TableId",
                        "The selected table does not exist."
                    );
                }
                // Table is already occupied
                else if (selectedTable.Status != "Available")
                {
                    ModelState.AddModelError(
                        "Order.TableId",
                        "This table is currently occupied. Please select another table."
                    );
                }
            }

            // CHECK FOOD SELECTION
            bool hasItem = Quantities.Any(q => q.Value > 0);

            if (!hasItem)
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one menu item."
                );
            }

            // STOP IF THERE ARE VALIDATION ERRORS
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // CREATE NEW ORDER
            var newOrder = new Order
            {
                TableId = Order.TableId,
                OrderDate = DateTime.Now,
                DineInDateTime = Order.DineInDateTime,
                Status = "Pending",
                TotalAmount = 0
            };

            // CALCULATE TOTAL
            decimal totalAmount = 0;

            // CREATE ORDER ITEMS
            foreach (var quantity in Quantities)
            {
                int menuId = quantity.Key;
                int selectedQuantity = quantity.Value;

                // Ignore quantity 0
                if (selectedQuantity <= 0)
                {
                    continue;
                }

                // Find menu item
                var menu = await _context.Menus
                    .FirstOrDefaultAsync(m => m.Id == menuId);

                // Menu item does not exist
                if (menu == null)
                {
                    continue;
                }

                // Calculate subtotal
                decimal subtotal = menu.Price * selectedQuantity;

                // Create OrderItem
                var orderItem = new OrderItem
                {
                    MenuId = menu.Id,
                    Quantity = selectedQuantity,
                    Price = menu.Price,
                    Subtotal = subtotal
                };

                // Add OrderItem to Order
                newOrder.OrderItems.Add(orderItem);

                // Add subtotal to total
                totalAmount += subtotal;
            }

            // SET ORDER TOTAL
            newOrder.TotalAmount = totalAmount;

            // CHANGE TABLE STATUS
            // AVAILABLE -> OCCUPIED
            if (selectedTable != null)
            {
                selectedTable.Status = "Occupied";
            }

            // ADD ORDER TO DATABASE
            _context.Orders.Add(newOrder);

            // SAVE ORDER AND TABLE STATUS
            await _context.SaveChangesAsync();

            // REDIRECT TO ORDER CONFIRMATION
            return RedirectToPage(
                "OrderConfirmation",
                new
                {
                    id = newOrder.OrderId
                });
        }
    }
}