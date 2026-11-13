using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Models;
using CustomerModel = RestaurantManagementSystem.Models.Customer;
using MenuModel = RestaurantManagementSystem.Models.Menu;

namespace RestaurantManagementSystem.Data
{
    /// <summary>
    /// Creates (and removes) dummy data so the admin dashboard and reports have
    /// something to show.
    ///
    /// How dummy rows are recognised, so they can be removed safely:
    ///   - Customers : email ends with @dummy.test
    ///   - Staff     : email ends with @dummy.test
    ///   - Orders    : SpecialRequest = "[DUMMY]"
    ///   - Reservations : belong to a dummy customer
    ///   - Menu items: same name AND same description as the ones listed below
    /// Your real data is never touched.
    /// </summary>
    public static class DbSeeder
    {
        public const string DummyEmailDomain = "@dummy.test";
        public const string DummyTag = "[DUMMY]";

        // Change these if your app uses different words for these statuses
        private const string OrderPending = "Pending";
        private const string OrderPreparing = "Preparing";
        private const string OrderServed = "Served";
        private const string OrderCancelled = "Cancelled";

        private const string ReservationReserved = "Reserved";
        private const string ReservationCompleted = "Completed";
        private const string ReservationCancelled = "Cancelled";

        private static readonly string[] PaymentMethods = { "Cash", "Card", "Online Banking" };

        private static readonly TimeSpan[] ReservationTimes =
        {
            new TimeSpan(12, 0, 0), new TimeSpan(12, 30, 0), new TimeSpan(13, 0, 0),
            new TimeSpan(13, 30, 0), new TimeSpan(18, 0, 0), new TimeSpan(18, 30, 0),
            new TimeSpan(19, 0, 0), new TimeSpan(19, 30, 0), new TimeSpan(20, 0, 0)
        };

        private static readonly string[] CustomerNames =
        {
            "Aisyah Rahman", "Daniel Lim", "Nurul Huda", "Kevin Tan", "Siti Aminah",
            "Raj Kumar", "Mei Ling Chong", "Haziq Ismail", "Priya Nair", "Jason Wong",
            "Farah Zulkifli", "Adam Hakim", "Sarah Lee", "Imran Yusof", "Chloe Ng"
        };

        // Dummy staff accounts (login password for all of them: Dummy123)
        private static readonly (string FullName, string Username)[] DummyStaff =
        {
            ("Hana Abdullah", "hana.staff"),
            ("Marcus Teo", "marcus.staff"),
            ("Nur Izzati", "izzati.staff"),
            ("Vikram Singh", "vikram.staff"),
            ("Lisa Chen", "lisa.staff")
        };

        // Categories used by the dummy menu (and therefore the View Menu filter buttons):
        // Pizza, Beverages, Sushi, Burger, Salad, Appetizer, Desserts (17 items)
        private static readonly (string Name, string Description, decimal Price, string Category)[] DummyMenus =
        {
            // Pizza
            ("Margherita Pizza", "Classic pizza with tomato sauce, mozzarella and fresh basil.", 24.90m, "Pizza"),
            ("Pepperoni Pizza", "Pizza topped with beef pepperoni and melted mozzarella.", 27.90m, "Pizza"),

            // Beverages
            ("Iced Lemon Tea", "Freshly brewed black tea with lemon, served over ice.", 5.90m, "Beverages"),
            ("Teh Tarik", "Classic pulled milk tea.", 4.50m, "Beverages"),
            ("Fresh Orange Juice", "Freshly squeezed orange juice.", 8.90m, "Beverages"),

            // Sushi
            ("Salmon Nigiri Set", "Six pieces of fresh salmon nigiri with wasabi and pickled ginger.", 22.90m, "Sushi"),
            ("California Roll", "Crab stick, avocado and cucumber rolled in rice and sesame seeds.", 18.90m, "Sushi"),
            ("Tamago Sushi", "Sweet Japanese egg omelette on seasoned sushi rice, wrapped with seaweed.", 12.90m, "Sushi"),

            // Burger
            ("Cheese Burger", "Crispy fish fillet with lettuce and tartar sauce in a toasted bun.", 16.90m, "Burger"),
            ("Crispy Chicken Burger", "Crispy fried chicken fillet with lettuce and spicy mayo in a toasted bun.", 17.90m, "Burger"),

            // Salad
            ("Caesar Salad", "Crisp romaine with parmesan, croutons and creamy Caesar dressing.", 15.90m, "Salad"),
            ("Garden Salad", "Mixed greens, cherry tomatoes, cucumber and balsamic vinaigrette.", 12.90m, "Salad"),

            // Appetizer
            ("Crispy Calamari", "Golden fried squid rings served with tartar sauce.", 12.90m, "Appetizer"),

            // Desserts
            ("Chocolate Lava Cake", "Warm chocolate cake with a molten centre and vanilla ice cream.", 11.90m, "Desserts"),
            ("Cendol", "Shaved ice with pandan jelly, red beans and gula Melaka.", 7.90m, "Desserts"),
            ("Vanilla Ice Cream", "Three scoops of creamy vanilla ice cream with a wafer.", 6.90m, "Desserts")
        };

        private static bool IsDummyMenu(MenuModel m) =>
            DummyMenus.Any(d =>
                string.Equals(d.Name, m.MenuName, StringComparison.OrdinalIgnoreCase) &&
                d.Description == m.Description);

        // ------------------------------------------------------------------
        // How many dummy rows exist right now
        // ------------------------------------------------------------------
        public static async Task<(int Menus, int Staff, int Customers, int Orders, int Reservations)> GetCountsAsync(
            ApplicationDbContext db)
        {
            var menus = (await db.Menus.ToListAsync()).Count(IsDummyMenu);
            int staff = await db.Staffs.CountAsync(s => s.Email.EndsWith(DummyEmailDomain));

            var customerIds = await db.Customers
                .Where(c => c.Email.EndsWith(DummyEmailDomain))
                .Select(c => c.CustomerId)
                .ToListAsync();

            int orders = await db.Orders.CountAsync(o => o.SpecialRequest == DummyTag);
            int reservations = await db.Reservations.CountAsync(r => customerIds.Contains(r.CustomerId));

            return (menus, staff, customerIds.Count, orders, reservations);
        }

        // ------------------------------------------------------------------
        // Called at app startup: seeds only if no dummy data exists yet
        // ------------------------------------------------------------------
        public static async Task EnsureSeededAsync(ApplicationDbContext db)
        {
            bool alreadySeeded = await db.Customers.AnyAsync(c => c.Email.EndsWith(DummyEmailDomain));
            if (alreadySeeded) return;

            await SeedAsync(db);
        }

        // ------------------------------------------------------------------
        // Remove every dummy row (and nothing else)
        // ------------------------------------------------------------------
        public static async Task RemoveAsync(ApplicationDbContext db)
        {
            // Orders (and their items) first
            var orders = await db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.SpecialRequest == DummyTag)
                .ToListAsync();

            foreach (var order in orders)
            {
                db.OrderItems.RemoveRange(order.OrderItems);
            }
            db.Orders.RemoveRange(orders);

            // Reservations of dummy customers
            var customerIds = await db.Customers
                .Where(c => c.Email.EndsWith(DummyEmailDomain))
                .Select(c => c.CustomerId)
                .ToListAsync();

            var reservations = await db.Reservations
                .Where(r => customerIds.Contains(r.CustomerId))
                .ToListAsync();
            db.Reservations.RemoveRange(reservations);

            await db.SaveChangesAsync();

            // Then the customers and menu items themselves
            var customers = await db.Customers
                .Where(c => customerIds.Contains(c.CustomerId))
                .ToListAsync();
            db.Customers.RemoveRange(customers);

            var staff = await db.Staffs
                .Where(s => s.Email.EndsWith(DummyEmailDomain))
                .ToListAsync();
            db.Staffs.RemoveRange(staff);

            var menus = (await db.Menus.ToListAsync()).Where(IsDummyMenu).ToList();
            db.Menus.RemoveRange(menus);

            await db.SaveChangesAsync();
        }

        // ------------------------------------------------------------------
        // Remove old dummy rows, then add a fresh set (dates are relative to today)
        // ------------------------------------------------------------------
        public static async Task<string> SeedAsync(ApplicationDbContext db)
        {
            await RemoveAsync(db);

            var rng = new Random(2026);   // fixed seed: same pattern every time
            var now = DateTime.Now;
            var today = now.Date;

            // ---------------- Menu items ----------------
            var existingNames = (await db.Menus.Select(m => m.MenuName).ToListAsync())
                .Select(n => n.ToLower())
                .ToHashSet();

            int menusAdded = 0;
            foreach (var d in DummyMenus)
            {
                if (existingNames.Contains(d.Name.ToLower())) continue;

                db.Menus.Add(new MenuModel
                {
                    MenuName = d.Name,
                    Description = d.Description,
                    Price = d.Price,
                    Category = d.Category
                });
                menusAdded++;
            }

            // ---------------- Staff ----------------
            var staffMembers = DummyStaff
                .Select(s => new Staff
                {
                    FullName = s.FullName,
                    Username = s.Username,
                    Email = s.Username + DummyEmailDomain,
                    Password = "Dummy123"
                })
                .ToList();
            db.Staffs.AddRange(staffMembers);

            // ---------------- Customers ----------------
            string[] tiers = { "Bronze", "Silver", "Gold" };
            var customers = new List<CustomerModel>();

            for (int i = 0; i < CustomerNames.Length; i++)
            {
                bool member = i % 2 == 0;

                customers.Add(new CustomerModel
                {
                    FullName = CustomerNames[i],
                    Email = CustomerNames[i].ToLower().Replace(' ', '.') + DummyEmailDomain,
                    Phone = $"01{rng.Next(0, 10)}-{rng.Next(1000000, 9999999)}",
                    Password = "Dummy@1234",
                    IsMember = member,
                    MembershipTier = member ? tiers[i % 3] : "Bronze",
                    LoyaltyPoints = member ? rng.Next(50, 1500) : 0,
                    MemberSince = member ? today.AddDays(-rng.Next(10, 200)) : null,
                    IsEmailVerified = true
                });
            }
            db.Customers.AddRange(customers);

            // ---------------- Tables (use yours; create some only if there are none) ----------------
            var tables = await db.RestaurantTables.OrderBy(t => t.RestaurantTableId).ToListAsync();

            if (tables.Count == 0)
            {
                for (int n = 1; n <= 8; n++)
                {
                    var table = new RestaurantTable
                    {
                        TableNumber = $"T{n:00}",
                        DiningArea = n <= 4 ? "Indoor" : "Outdoor",
                        Capacity = n % 2 == 0 ? 4 : 2,
                        Status = "Available"
                    };
                    db.RestaurantTables.Add(table);
                    tables.Add(table);
                }
            }

            // Save so the new menu items, customers and tables get their IDs
            await db.SaveChangesAsync();

            // Use every menu item (yours + dummy), in a fixed random order so some are more popular
            var menus = (await db.Menus.ToListAsync())
                .OrderBy(_ => rng.Next())
                .ToList();

            var orders = new List<Order>();
            var reservations = new List<Reservation>();

            // ---------------- Local helpers ----------------
            Order BuildOrder(DateTime dineIn, string status)
            {
                var placed = dineIn.AddHours(-rng.Next(1, 25));
                if (placed > now) placed = now.AddMinutes(-rng.Next(5, 120));

                bool paid = status == OrderServed;

                var order = new Order
                {
                    CustomerId = customers[rng.Next(customers.Count)].CustomerId,
                    TableId = tables[rng.Next(tables.Count)].RestaurantTableId,
                    OrderDate = placed,
                    DineInDateTime = dineIn,
                    Status = status,
                    PaymentStatus = paid ? "Paid" : "Unpaid",
                    PaymentMethod = paid ? PaymentMethods[rng.Next(PaymentMethods.Length)] : null,
                    SpecialRequest = DummyTag
                };

                int lines = Math.Min(rng.Next(1, 5), menus.Count);
                var chosen = new HashSet<int>();
                decimal total = 0;

                while (order.OrderItems.Count < lines)
                {
                    // Lower indexes are picked more often
                    int idx = (int)(menus.Count * Math.Pow(rng.NextDouble(), 1.8));
                    if (idx >= menus.Count) idx = menus.Count - 1;
                    if (!chosen.Add(idx)) continue;

                    var menu = menus[idx];
                    int qty = rng.Next(1, 4);
                    decimal subtotal = menu.Price * qty;

                    order.OrderItems.Add(new OrderItem
                    {
                        MenuId = menu.Id,
                        Quantity = qty,
                        Size = "Small",
                        Price = menu.Price,
                        Subtotal = subtotal
                    });

                    total += subtotal;
                }

                order.TotalAmount = total;
                return order;
            }

            void AddReservation(DateTime date, RestaurantTable table, TimeSpan time, string status)
            {
                var created = date.AddDays(-rng.Next(1, 4)).AddHours(rng.Next(9, 21));
                if (created > now) created = now.AddMinutes(-rng.Next(5, 600));

                reservations.Add(new Reservation
                {
                    CustomerId = customers[rng.Next(customers.Count)].CustomerId,
                    RestaurantTableId = table.RestaurantTableId,
                    ReservationDate = date,
                    ReservationTime = time,
                    NumberOfGuests = rng.Next(1, Math.Min(Math.Max(table.Capacity, 1), 20) + 1),
                    Status = status,
                    CreatedAt = created
                });
            }

            // ---------------- Orders: past 45 days ----------------
            int[] hours = { 12, 13, 14, 18, 19, 20, 21 };

            for (int d = 45; d >= 1; d--)
            {
                var day = today.AddDays(-d);
                bool weekend = day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;
                int count = rng.Next(0, 3) + (weekend ? 1 : 0);

                for (int k = 0; k < count; k++)
                {
                    var dineIn = day
                        .AddHours(hours[rng.Next(hours.Length)])
                        .AddMinutes(rng.Next(0, 4) * 15);

                    string status = rng.NextDouble() < 0.88 ? OrderServed : OrderCancelled;
                    orders.Add(BuildOrder(dineIn, status));
                }
            }

            // ---------------- Orders: today (5 count on the dashboard, 1 cancelled) ----------------
            var todayOrders = new (int Hour, int Minute, string Status)[]
            {
                (11, 30, OrderServed),
                (12, 45, OrderServed),
                (13, 15, OrderPreparing),
                (18, 30, OrderPending),
                (19, 45, OrderPending),
                (20, 0, OrderCancelled)
            };

            foreach (var o in todayOrders)
            {
                orders.Add(BuildOrder(today.AddHours(o.Hour).AddMinutes(o.Minute), o.Status));
            }

            // ---------------- Reservations: past 45 days ----------------
            for (int d = 45; d >= 1; d--)
            {
                if (rng.NextDouble() > 0.6) continue;

                var date = today.AddDays(-d);
                int count = rng.Next(1, 3);

                foreach (var table in tables.OrderBy(_ => rng.Next()).Take(count))
                {
                    string status = rng.NextDouble() < 0.8 ? ReservationCompleted : ReservationCancelled;
                    AddReservation(date, table, ReservationTimes[rng.Next(ReservationTimes.Length)], status);
                }
            }

            // ---------------- Reservations: today (4 active on 4 different tables + 1 cancelled) ----------------
            var todayTables = tables.OrderBy(_ => rng.Next()).Take(5).ToList();
            var todayTimes = new[]
            {
                new TimeSpan(12, 0, 0), new TimeSpan(13, 30, 0), new TimeSpan(18, 30, 0),
                new TimeSpan(19, 30, 0), new TimeSpan(20, 0, 0)
            };

            for (int i = 0; i < todayTables.Count; i++)
            {
                string status = i == todayTables.Count - 1 ? ReservationCancelled : ReservationReserved;
                AddReservation(today, todayTables[i], todayTimes[i], status);
            }

            // ---------------- Reservations: next 7 days ----------------
            for (int d = 1; d <= 7; d++)
            {
                var date = today.AddDays(d);
                int count = rng.Next(1, 3);

                foreach (var table in tables.OrderBy(_ => rng.Next()).Take(count))
                {
                    AddReservation(date, table, ReservationTimes[rng.Next(ReservationTimes.Length)], ReservationReserved);
                }
            }

            db.Orders.AddRange(orders);
            db.Reservations.AddRange(reservations);
            await db.SaveChangesAsync();

            int served = orders.Count(o => o.Status == OrderServed);

            return $"Added {menusAdded} menu items, {staffMembers.Count} staff accounts, {customers.Count} customers, " +
                   $"{orders.Count} orders ({served} served) and {reservations.Count} reservations.";
        }
    }
}