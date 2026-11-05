using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Admin.Reports
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------------
        // Access control: only the logged-in admin may view / export
        // ---------------------------------------------------------------
        private bool IsAdmin =>
            HttpContext.Session.GetString("AdminLoggedIn") == "true";

        private IActionResult RedirectToLogin() =>
            RedirectToPage("/Index", new { login = "true" });

        // ---------------------------------------------------------------
        // Filters
        // ---------------------------------------------------------------
        [BindProperty(SupportsGet = true)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateTo { get; set; }

        public bool InvalidRange =>
            DateFrom.HasValue && DateTo.HasValue &&
            DateFrom.Value.Date > DateTo.Value.Date;

        // Chart pictures sent by the browser when the PDF button is pressed
        [BindProperty] public string? RevenueChartImage { get; set; }
        [BindProperty] public string? SalesChartImage { get; set; }
        [BindProperty] public string? ReservationPieImage { get; set; }
        [BindProperty] public string? ReservationBarImage { get; set; }
        [BindProperty] public string? PopularChartImage { get; set; }

        // ---------------------------------------------------------------
        // Report data
        // ---------------------------------------------------------------
        // Slice colours shared by the pie charts and the status dots in the view
        public string[] Palette { get; } = new[]
        {
            "#4DA8DA", "#16A085", "#E67E22", "#8E44AD",
            "#D4AC0D", "#C0392B", "#2980B9", "#718096"
        };

        public class DailyRow
        {
            public DateTime Date { get; set; }
            public int Orders { get; set; }
            public int Items { get; set; }
            public decimal Revenue { get; set; }
        }

        public class ItemRow
        {
            public string MenuName { get; set; } = "";
            public int Quantity { get; set; }
            public decimal Amount { get; set; }
            public decimal SharePercent { get; set; }
        }

        public class StatusRow
        {
            public string Status { get; set; } = "";
            public int Count { get; set; }
            public int Guests { get; set; }
            public decimal SharePercent { get; set; }
        }

        public class DayCountRow
        {
            public DateTime Date { get; set; }
            public int Count { get; set; }
        }

        public class SliceRow
        {
            public string Name { get; set; } = "";
            public decimal Value { get; set; }
        }

        // Served orders / revenue
        public List<Order> ServedOrders { get; set; } = new();
        public int TotalServedOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalItemsSold { get; set; }
        public decimal AverageOrderValue =>
            TotalServedOrders > 0 ? TotalRevenue / TotalServedOrders : 0;
        public List<DailyRow> DailyRevenue { get; set; } = new();

        // Sales report + popular items
        public List<ItemRow> ItemsByAmount { get; set; } = new();
        public List<ItemRow> PopularItems { get; set; } = new();
        public List<SliceRow> SalesShare { get; set; } = new();

        // Reservations
        public int TotalReservations { get; set; }
        public int TotalGuests { get; set; }
        public decimal AveragePartySize =>
            TotalReservations > 0 ? (decimal)TotalGuests / TotalReservations : 0;
        public List<StatusRow> ReservationStatuses { get; set; } = new();
        public List<DayCountRow> ReservationsByDay { get; set; } = new();

        // Everything the charts need, serialised to JSON by the view
        public object ChartData => new
        {
            palette = Palette,
            revenue = new
            {
                labels = DailyRevenue.Select(d => d.Date.ToString("dd MMM")).ToList(),
                values = DailyRevenue.Select(d => d.Revenue).ToList()
            },
            salesShare = new
            {
                labels = SalesShare.Select(s => s.Name).ToList(),
                values = SalesShare.Select(s => s.Value).ToList()
            },
            reservationStatus = new
            {
                labels = ReservationStatuses.Select(s => s.Status).ToList(),
                values = ReservationStatuses.Select(s => s.Count).ToList()
            },
            reservationDaily = new
            {
                labels = ReservationsByDay.Select(d => d.Date.ToString("dd MMM")).ToList(),
                values = ReservationsByDay.Select(d => d.Count).ToList()
            },
            popular = new
            {
                labels = PopularItems.Select(i => i.MenuName).ToList(),
                values = PopularItems.Select(i => i.Quantity).ToList()
            }
        };

        // ---------------------------------------------------------------
        // Page
        // ---------------------------------------------------------------
        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAdmin) return RedirectToLogin();

            if (InvalidRange)
            {
                // The view shows the error message; no data is loaded
                return Page();
            }

            await LoadReportDataAsync();
            return Page();
        }

        private async Task<List<Order>> GetServedOrdersAsync()
        {
            var query = _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Served")
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Menu)
                .AsQueryable();

            if (DateFrom.HasValue)
            {
                DateTime from = DateFrom.Value.Date;
                query = query.Where(o => o.DineInDateTime >= from);
            }

            if (DateTo.HasValue)
            {
                DateTime endExclusive = DateTo.Value.Date.AddDays(1);
                query = query.Where(o => o.DineInDateTime < endExclusive);
            }

            return await query
                .OrderByDescending(o => o.DineInDateTime)
                .ToListAsync();
        }

        private async Task LoadReportDataAsync()
        {
            // ----- Served orders and revenue -----
            ServedOrders = await GetServedOrdersAsync();

            TotalServedOrders = ServedOrders.Count;
            TotalRevenue = ServedOrders.Sum(o => o.TotalAmount);

            var lines = ServedOrders
                .SelectMany(o => o.OrderItems ?? new List<OrderItem>())
                .ToList();

            TotalItemsSold = lines.Sum(i => i.Quantity);

            DailyRevenue = ServedOrders
                .GroupBy(o => o.DineInDateTime.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyRow
                {
                    Date = g.Key,
                    Orders = g.Count(),
                    Items = g.Sum(o => o.OrderItems?.Sum(i => i.Quantity) ?? 0),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            // ----- Sales per menu item -----
            var itemSales = lines
                .GroupBy(i => i.Menu?.MenuName ?? "Unknown item")
                .Select(g => new ItemRow
                {
                    MenuName = g.Key,
                    Quantity = g.Sum(i => i.Quantity),
                    Amount = g.Sum(i => i.Price * i.Quantity)
                })
                .ToList();

            decimal itemsTotal = itemSales.Sum(i => i.Amount);

            foreach (var item in itemSales)
            {
                item.SharePercent = itemsTotal > 0
                    ? Math.Round(item.Amount / itemsTotal * 100, 1)
                    : 0;
            }

            ItemsByAmount = itemSales
                .OrderByDescending(i => i.Amount)
                .ToList();

            PopularItems = itemSales
                .OrderByDescending(i => i.Quantity)
                .ThenByDescending(i => i.Amount)
                .Take(10)
                .ToList();

            // Pie chart: the top 6 items, everything else grouped as "Others"
            SalesShare = new List<SliceRow>();

            if (itemsTotal > 0)
            {
                SalesShare = ItemsByAmount
                    .Take(6)
                    .Select(i => new SliceRow { Name = i.MenuName, Value = i.Amount })
                    .ToList();

                decimal others = ItemsByAmount.Skip(6).Sum(i => i.Amount);

                if (others > 0)
                {
                    SalesShare.Add(new SliceRow { Name = "Others", Value = others });
                }
            }

            // ----- Reservations (filtered by reservation date) -----
            var reservationQuery = _context.Reservations.AsNoTracking();

            if (DateFrom.HasValue)
            {
                DateTime from = DateFrom.Value.Date;
                reservationQuery = reservationQuery
                    .Where(r => r.ReservationDate >= from);
            }

            if (DateTo.HasValue)
            {
                DateTime endExclusive = DateTo.Value.Date.AddDays(1);
                reservationQuery = reservationQuery
                    .Where(r => r.ReservationDate < endExclusive);
            }

            var reservations = await reservationQuery
                .Select(r => new
                {
                    r.Status,
                    r.ReservationDate,
                    r.NumberOfGuests
                })
                .ToListAsync();

            TotalReservations = reservations.Count;
            TotalGuests = reservations.Sum(r => r.NumberOfGuests);

            ReservationStatuses = reservations
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Status) ? "Unknown" : r.Status)
                .Select(g => new StatusRow
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Guests = g.Sum(r => r.NumberOfGuests),
                    SharePercent = TotalReservations > 0
                        ? Math.Round((decimal)g.Count() / TotalReservations * 100, 1)
                        : 0
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            ReservationsByDay = reservations
                .GroupBy(r => r.ReservationDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DayCountRow { Date = g.Key, Count = g.Count() })
                .ToList();
        }

        // ---------------------------------------------------------------
        // PDF export (POST so the chart pictures can travel with it)
        // section = all | revenue | sales | reservations | popular | orders
        // ---------------------------------------------------------------
        public async Task<IActionResult> OnPostExportPdfAsync(string? section)
        {
            if (!IsAdmin) return RedirectToLogin();

            if (InvalidRange)
            {
                return RedirectToPage(new
                {
                    DateFrom = DateFrom?.ToString("yyyy-MM-dd"),
                    DateTo = DateTo?.ToString("yyyy-MM-dd")
                });
            }

            await LoadReportDataAsync();

            section = (section ?? "all").Trim().ToLowerInvariant();

            string[] allowed = { "all", "revenue", "sales", "reservations", "popular", "orders" };
            if (!allowed.Contains(section)) section = "all";

            byte[] pdf = BuildPdf(section);

            string fileName = section switch
            {
                "revenue" => "SalesRevenueReport.pdf",
                "sales" => "SalesReport.pdf",
                "reservations" => "ReservationStatisticsReport.pdf",
                "popular" => "PopularMenuItemsReport.pdf",
                "orders" => "ServedOrdersReport.pdf",
                _ => "RestaurantFullReport.pdf"
            };

            return File(pdf, "application/pdf", fileName);
        }

        // Turns "data:image/png;base64,...." into PNG bytes (or null if it is not a PNG)
        private static byte[]? DecodeChart(string? dataUrl)
        {
            const string prefix = "data:image/png;base64,";

            if (string.IsNullOrWhiteSpace(dataUrl) ||
                !dataUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(dataUrl.Substring(prefix.Length));

                byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

                if (bytes.Length < signature.Length || bytes.Length > 3_000_000)
                {
                    return null;
                }

                for (int i = 0; i < signature.Length; i++)
                {
                    if (bytes[i] != signature[i]) return null;
                }

                return bytes;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private const string Brand = "#2E8BC0";
        private const string SoftBlue = "#E3F2FD";
        private const string SoftBorder = "#E3EEF5";

        private byte[] BuildPdf(string section)
        {
            string title = section switch
            {
                "revenue" => "Sales Revenue Report",
                "sales" => "Sales Report",
                "reservations" => "Reservation Statistics",
                "popular" => "Popular Menu Items",
                "orders" => "Served Orders Report",
                _ => "Restaurant Performance Report"
            };

            byte[]? revenueImg = DecodeChart(RevenueChartImage);
            byte[]? salesImg = DecodeChart(SalesChartImage);
            byte[]? resPieImg = DecodeChart(ReservationPieImage);
            byte[]? resBarImg = DecodeChart(ReservationBarImage);
            byte[]? popularImg = DecodeChart(PopularChartImage);

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item()
                            .Text("MyCafe - Restaurant Reservation and Order Management System")
                            .FontSize(11)
                            .SemiBold();

                        column.Item()
                            .PaddingTop(4)
                            .Text(title)
                            .FontSize(20)
                            .Bold()
                            .FontColor(Brand);

                        column.Item()
                            .PaddingTop(4)
                            .Text(
                                $"Period: {DateFrom?.ToString("dd/MM/yyyy") ?? "Beginning"} " +
                                $"to {DateTo?.ToString("dd/MM/yyyy") ?? "Latest"}")
                            .FontSize(10);

                        column.Item()
                            .PaddingTop(8)
                            .LineHorizontal(1)
                            .LineColor(Colors.Blue.Medium);
                    });

                    page.Content().PaddingTop(10).Column(column =>
                    {
                        column.Spacing(12);

                        bool all = section == "all";
                        bool first = true;

                        void AddSection(string key, string heading, Action<IContainer> body)
                        {
                            if (!all && section != key) return;

                            if (all && !first)
                            {
                                column.Item().PageBreak();
                            }

                            first = false;

                            column.Item().Column(inner =>
                            {
                                inner.Spacing(10);

                                if (all)
                                {
                                    inner.Item()
                                        .Text(heading)
                                        .FontSize(15)
                                        .Bold()
                                        .FontColor(Brand);
                                }

                                inner.Item().Element(body);
                            });
                        }

                        AddSection("revenue", "1. Sales Revenue",
                            c => ComposeRevenue(c, revenueImg));

                        AddSection("sales", "2. Sales Report",
                            c => ComposeSales(c, salesImg));

                        AddSection("reservations", "3. Reservation Statistics",
                            c => ComposeReservations(c, resPieImg, resBarImg));

                        AddSection("popular", "4. Popular Menu Items",
                            c => ComposePopular(c, popularImg));

                        AddSection("orders", "5. Served Orders",
                            c => ComposeOrders(c));
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Generated on ");
                            text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                            text.Span(" | Page ");
                            text.CurrentPageNumber();
                        });
                });
            }).GeneratePdf();
        }

        // ----- PDF building blocks -----
        private static IContainer PdfCell(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(SoftBorder)
                .Padding(5)
                .DefaultTextStyle(style => style.FontSize(9));
        }

        // Headings from index "rightFrom" onwards are right aligned (number columns)
        private static void AddHeader(
            TableDescriptor table,
            int rightFrom,
            params string[] headings)
        {
            for (int i = 0; i < headings.Length; i++)
            {
                IContainer cell = table.Cell().Background(SoftBlue).Padding(5);

                if (i >= rightFrom) cell = cell.AlignRight();

                cell.Text(headings[i]).Bold().FontSize(9);
            }
        }

        private static void TextCell(TableDescriptor table, string text, bool bold = false)
        {
            var t = table.Cell().Element(PdfCell).Text(text);
            if (bold) t.Bold();
        }

        private static void NumCell(TableDescriptor table, string text, bool bold = false)
        {
            var t = table.Cell().Element(PdfCell).AlignRight().Text(text);
            if (bold) t.Bold();
        }

        private static void Kpi(RowDescriptor row, string label, string value)
        {
            row.RelativeItem()
                .Border(1)
                .BorderColor(SoftBorder)
                .Background("#F6FAFD")
                .Padding(10)
                .Column(c =>
                {
                    c.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(2).Text(value).FontSize(16).Bold();
                });
        }

        private static void EmptyNote(IContainer container, string message)
        {
            container
                .Padding(10)
                .Text(message)
                .FontColor(Colors.Grey.Darken1);
        }

        // ----- 1. Sales revenue -----
        private void ComposeRevenue(IContainer container, byte[]? chart)
        {
            container.Column(col =>
            {
                col.Spacing(12);

                col.Item().Row(row =>
                {
                    row.Spacing(10);
                    Kpi(row, "Total revenue", $"RM {TotalRevenue:N2}");
                    Kpi(row, "Served orders", TotalServedOrders.ToString());
                    Kpi(row, "Items sold", TotalItemsSold.ToString());
                    Kpi(row, "Average order value", $"RM {AverageOrderValue:N2}");
                });

                if (chart != null)
                {
                    col.Item().AlignCenter().MaxWidth(560).Image(chart);
                }

                if (!DailyRevenue.Any())
                {
                    col.Item().Element(c => EmptyNote(c, "No served orders in this period."));
                    return;
                }

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.4f);
                    });

                    AddHeader(table, 1, "Date", "Orders", "Items sold", "Revenue (RM)");

                    foreach (var day in DailyRevenue)
                    {
                        TextCell(table, day.Date.ToString("dd/MM/yyyy"));
                        NumCell(table, day.Orders.ToString());
                        NumCell(table, day.Items.ToString());
                        NumCell(table, day.Revenue.ToString("N2"));
                    }

                    TextCell(table, "Total", true);
                    NumCell(table, TotalServedOrders.ToString(), true);
                    NumCell(table, TotalItemsSold.ToString(), true);
                    NumCell(table, TotalRevenue.ToString("N2"), true);
                });
            });
        }

        // ----- 2. Sales report (per menu item) -----
        private void ComposeSales(IContainer container, byte[]? chart)
        {
            if (!ItemsByAmount.Any())
            {
                EmptyNote(container, "No menu sales in this period.");
                return;
            }

            container.Row(row =>
            {
                row.Spacing(15);

                row.RelativeItem(3).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(3f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(0.9f);
                    });

                    AddHeader(table, 2, "#", "Menu item", "Qty sold", "Sales (RM)", "Share");

                    int rank = 1;

                    foreach (var item in ItemsByAmount)
                    {
                        TextCell(table, rank.ToString());
                        TextCell(table, item.MenuName);
                        NumCell(table, item.Quantity.ToString());
                        NumCell(table, item.Amount.ToString("N2"));
                        NumCell(table, $"{item.SharePercent:0.#}%");
                        rank++;
                    }

                    table.Cell().ColumnSpan(2).Element(PdfCell).Text("Total").Bold();
                    NumCell(table, ItemsByAmount.Sum(i => i.Quantity).ToString(), true);
                    NumCell(table, ItemsByAmount.Sum(i => i.Amount).ToString("N2"), true);
                    NumCell(table, "", true);
                });

                if (chart != null)
                {
                    row.RelativeItem(2).AlignMiddle().Image(chart);
                }
            });
        }

        // ----- 3. Reservation statistics -----
        private void ComposeReservations(IContainer container, byte[]? pie, byte[]? bar)
        {
            container.Column(col =>
            {
                col.Spacing(12);

                col.Item().Row(row =>
                {
                    row.Spacing(10);
                    Kpi(row, "Total reservations", TotalReservations.ToString());
                    Kpi(row, "Total guests", TotalGuests.ToString());
                    Kpi(row, "Average party size", AveragePartySize.ToString("0.0"));
                });

                if (pie != null || bar != null)
                {
                    col.Item().Row(row =>
                    {
                        row.Spacing(15);

                        if (pie != null) row.RelativeItem().Image(pie);
                        if (bar != null) row.RelativeItem().Image(bar);
                    });
                }

                if (!ReservationStatuses.Any())
                {
                    col.Item().Element(c => EmptyNote(c, "No reservations in this period."));
                    return;
                }

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1f);
                    });

                    AddHeader(table, 1, "Status", "Reservations", "Guests", "Share");

                    foreach (var status in ReservationStatuses)
                    {
                        TextCell(table, status.Status);
                        NumCell(table, status.Count.ToString());
                        NumCell(table, status.Guests.ToString());
                        NumCell(table, $"{status.SharePercent:0.#}%");
                    }

                    TextCell(table, "Total", true);
                    NumCell(table, TotalReservations.ToString(), true);
                    NumCell(table, TotalGuests.ToString(), true);
                    NumCell(table, "100%", true);
                });
            });
        }

        // ----- 4. Popular menu items -----
        private void ComposePopular(IContainer container, byte[]? chart)
        {
            if (!PopularItems.Any())
            {
                EmptyNote(container, "No menu sales in this period.");
                return;
            }

            container.Column(col =>
            {
                col.Spacing(12);

                if (chart != null)
                {
                    col.Item().AlignCenter().MaxWidth(520).Image(chart);
                }

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn(3f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.3f);
                    });

                    AddHeader(table, 2, "Rank", "Menu item", "Qty sold", "Sales (RM)");

                    int rank = 1;

                    foreach (var item in PopularItems)
                    {
                        TextCell(table, rank.ToString());
                        TextCell(table, item.MenuName);
                        NumCell(table, item.Quantity.ToString());
                        NumCell(table, item.Amount.ToString("N2"));
                        rank++;
                    }
                });
            });
        }

        // ----- 5. Served orders -----
        private void ComposeOrders(IContainer container)
        {
            if (!ServedOrders.Any())
            {
                EmptyNote(container, "No served orders in this period.");
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.7f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(2.5f);
                    columns.RelativeColumn(0.9f);
                });

                AddHeader(table, 5, "Order ID", "Table", "Date", "Time", "Items", "Total (RM)");

                foreach (var order in ServedOrders)
                {
                    string items = string.Join(
                        ", ",
                        (order.OrderItems ?? new List<OrderItem>()).Select(item =>
                            $"{item.Menu?.MenuName ?? "Unknown"} x {item.Quantity}"));

                    TextCell(table, $"#{order.OrderId}");
                    TextCell(table, order.Table?.TableNumber ?? "N/A");
                    TextCell(table, order.DineInDateTime.ToString("dd/MM/yyyy"));
                    TextCell(table, order.DineInDateTime.ToString("hh:mm tt"));
                    TextCell(table, items);
                    NumCell(table, order.TotalAmount.ToString("N2"));
                }

                table.Cell().ColumnSpan(5).Element(PdfCell).Text("Total Revenue").Bold();
                NumCell(table, TotalRevenue.ToString("N2"), true);
            });
        }
    }
}