using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using StaffModel = RestaurantManagementSystem.Models.Staff;

namespace RestaurantManagementSystem.Pages.Admin.ManageStaff
{
    public class ManageStaffModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageStaffModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<StaffModel> StaffList { get; set; } = new List<StaffModel>();

        // Search text (name or email), bound from the query string
        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        // Total number of staff accounts before any search
        public int TotalCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check admin login
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }

            TotalCount = await _context.Staffs.CountAsync();

            var query = _context.Staffs.AsQueryable();

            // Search by full name or email (case-insensitive, partial match)
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim().ToLower();
                query = query.Where(s =>
                    s.FullName.ToLower().Contains(term) ||
                    s.Email.ToLower().Contains(term));
            }

            StaffList = await query
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            return Page();
        }
    }
}