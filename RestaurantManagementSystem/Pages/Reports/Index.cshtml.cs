using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestaurantManagementSystem.Models;
using RestaurantManagementSystem.Data;

namespace RestaurantManagementSystem.Pages.Reports
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateTo { get; set; }

        public List<Order> ServedOrders { get; set; } = new();

        public decimal TotalRevenue { get; set; }

        public int TotalServedOrders { get; set; }

        public int TotalItemsSold { get; set; }

        public List<MenuSalesRow> BestSellingItems { get; set; } = new();

        public class MenuSalesRow
        {
            public string MenuName { get; set; } = "";

            public int QuantitySold { get; set; }

            public decimal SalesAmount { get; set; }
        }

        // Display reports on the web page
        public async Task OnGetAsync()
        {
            if (DateFrom.HasValue && DateTo.HasValue &&
                DateFrom.Value.Date > DateTo.Value.Date)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "From Date cannot be later than To Date.");

                return;
            }

            await LoadReportDataAsync();
        }

        // Retrieve served orders for the selected dates
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
                query = query.Where(o =>
                    o.DineInDateTime >= DateFrom.Value.Date);
            }

            if (DateTo.HasValue)
            {
                DateTime endExclusive =
                    DateTo.Value.Date.AddDays(1);

                query = query.Where(o =>
                    o.DineInDateTime < endExclusive);
            }

            return await query
                .OrderByDescending(o => o.DineInDateTime)
                .ToListAsync();
        }

        // Load the data used by all three reports
        private async Task LoadReportDataAsync()
        {
            ServedOrders = await GetServedOrdersAsync();

            TotalServedOrders = ServedOrders.Count;

            TotalRevenue = ServedOrders.Sum(o => o.TotalAmount);

            var items = ServedOrders
                .SelectMany(o =>
                    o.OrderItems ?? new List<OrderItem>())
                .ToList();

            TotalItemsSold = items.Sum(i => i.Quantity);

            BestSellingItems = items
                .GroupBy(i => i.Menu?.MenuName ?? "Unknown item")
                .Select(g => new MenuSalesRow
                {
                    MenuName = g.Key,
                    QuantitySold = g.Sum(i => i.Quantity),

                    // Assumes OrderItem has a Price property.
                    SalesAmount = g.Sum(i =>
                        i.Price * i.Quantity)
                })
                .OrderByDescending(i => i.QuantitySold)
                .ToList();
        }

        // Common PDF layout
        private byte[] CreatePdf(
            string title,
            Action<IContainer> content)
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);

                    page.Header().Column(column =>
                    {
                        column.Item()
                            .Text("Restaurant Reservation and Order Management System")
                            .FontSize(12)
                            .SemiBold();

                        column.Item()
                            .PaddingTop(5)
                            .Text(title)
                            .FontSize(20)
                            .Bold();

                        column.Item()
                            .PaddingTop(5)
                            .Text(
                                $"Period: {DateFrom?.ToString("dd/MM/yyyy") ?? "Beginning"} " +
                                $"to {DateTo?.ToString("dd/MM/yyyy") ?? "Latest"}")
                            .FontSize(10);

                        column.Item()
                            .PaddingTop(10)
                            .LineHorizontal(1);
                    });

                    page.Content()
                        .PaddingTop(10)
                        .Element(content);

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Generated on ");
                            text.Span(
                                DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                            text.Span(" | Page ");
                            text.CurrentPageNumber();
                        });
                });
            }).GeneratePdf();
        }

        // Common PDF table cell styling
        private static IContainer PdfCell(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5)
                .DefaultTextStyle(style => style.FontSize(9));
        }

        // Common PDF table heading
        private static void AddHeader(
            TableDescriptor table,
            params string[] headings)
        {
            foreach (var heading in headings)
            {
                table.Cell()
                    .Background(Colors.Grey.Lighten2)
                    .Padding(5)
                    .Text(heading)
                    .Bold()
                    .FontSize(9);
            }
        }

        // REPORT 1: Served Orders PDF
        public async Task<IActionResult>
            OnGetDownloadServedOrdersPdfAsync()
        {
            if (DateFrom.HasValue && DateTo.HasValue &&
                DateFrom.Value.Date > DateTo.Value.Date)
            {
                return BadRequest(
                    "From Date cannot be later than To Date.");
            }

            var orders = await GetServedOrdersAsync();

            var pdf = CreatePdf(
                "Served Orders Report",
                container => container.Table(table =>
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

                    AddHeader(
                        table,
                        "Order ID",
                        "Table",
                        "Date",
                        "Time",
                        "Items",
                        "Total (RM)");

                    foreach (var order in orders)
                    {
                        string items = string.Join(
                            ", ",
                            order.OrderItems.Select(item =>
                                $"{item.Menu?.MenuName ?? "Unknown"} x {item.Quantity}"));

                        table.Cell().Element(PdfCell)
                            .Text($"#{order.OrderId}");

                        table.Cell().Element(PdfCell)
                            .Text(order.Table?.TableNumber ?? "N/A");

                        table.Cell().Element(PdfCell)
                            .Text(order.DineInDateTime.ToString("dd/MM/yyyy"));

                        table.Cell().Element(PdfCell)
                            .Text(order.DineInDateTime.ToString("hh:mm tt"));

                        table.Cell().Element(PdfCell)
                            .Text(items);

                        table.Cell().Element(PdfCell)
                            .Text(order.TotalAmount.ToString("N2"));
                    }

                    table.Cell().ColumnSpan(5).Element(PdfCell)
                        .Text("Total Revenue").Bold();

                    table.Cell().Element(PdfCell)
                        .Text(orders.Sum(o => o.TotalAmount).ToString("N2"))
                        .Bold();
                }));

            return File(
                pdf,
                "application/pdf",
                "ServedOrdersReport.pdf");
        }

        // REPORT 2: Sales Revenue PDF
        public async Task<IActionResult>
            OnGetDownloadSalesRevenuePdfAsync()
        {
            if (DateFrom.HasValue && DateTo.HasValue &&
                DateFrom.Value.Date > DateTo.Value.Date)
            {
                return BadRequest(
                    "From Date cannot be later than To Date.");
            }

            var orders = await GetServedOrdersAsync();

            int totalItems = orders
                .SelectMany(o =>
                    o.OrderItems ?? new List<OrderItem>())
                .Sum(item => item.Quantity);

            decimal revenue = orders.Sum(o => o.TotalAmount);

            var pdf = CreatePdf(
                "Sales Revenue Report",
                container => container.Column(column =>
                {
                    column.Spacing(15);

                    column.Item().Text(
                        $"Number of Served Orders: {orders.Count}");

                    column.Item().Text(
                        $"Total Items Sold: {totalItems}");

                    column.Item()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(15)
                        .Text($"Total Revenue: RM {revenue:N2}")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Green.Darken2);
                }));

            return File(
                pdf,
                "application/pdf",
                "SalesRevenueReport.pdf");
        }

        // REPORT 3: Best-Selling Menu Items PDF
        public async Task<IActionResult>
            OnGetDownloadBestSellingPdfAsync()
        {
            if (DateFrom.HasValue && DateTo.HasValue &&
                DateFrom.Value.Date > DateTo.Value.Date)
            {
                return BadRequest(
                    "From Date cannot be later than To Date.");
            }

            var orders = await GetServedOrdersAsync();

            var items = orders
                .SelectMany(o =>
                    o.OrderItems ?? new List<OrderItem>())
                .GroupBy(i => i.Menu?.MenuName ?? "Unknown item")
                .Select(g => new MenuSalesRow
                {
                    MenuName = g.Key,
                    QuantitySold = g.Sum(i => i.Quantity),
                    SalesAmount = g.Sum(i =>
                        i.Price * i.Quantity)
                })
                .OrderByDescending(i => i.QuantitySold)
                .ToList();

            var pdf = CreatePdf(
                "Best-Selling Menu Items Report",
                container => container.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(0.5f);
                        columns.RelativeColumn(3f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.2f);
                    });

                    AddHeader(
                        table,
                        "Rank",
                        "Menu Item",
                        "Quantity Sold",
                        "Sales Amount (RM)");

                    int rank = 1;

                    foreach (var item in items)
                    {
                        table.Cell().Element(PdfCell)
                            .Text(rank.ToString());

                        table.Cell().Element(PdfCell)
                            .Text(item.MenuName);

                        table.Cell().Element(PdfCell)
                            .Text(item.QuantitySold.ToString());

                        table.Cell().Element(PdfCell)
                            .Text(item.SalesAmount.ToString("N2"));

                        rank++;
                    }

                    table.Cell().ColumnSpan(2).Element(PdfCell)
                        .Text("Total").Bold();

                    table.Cell().Element(PdfCell)
                        .Text(items.Sum(i => i.QuantitySold).ToString())
                        .Bold();

                    table.Cell().Element(PdfCell)
                        .Text(items.Sum(i => i.SalesAmount).ToString("N2"))
                        .Bold();
                }));

            return File(
                pdf,
                "application/pdf",
                "BestSellingItemsReport.pdf");
        }
    }
}