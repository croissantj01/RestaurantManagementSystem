using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;
using System.Text.Json;

namespace RestaurantManagementSystem.Pages.Customer
{
    public class AnnouncementsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AnnouncementsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Announcement> Announcements { get; set; } = new List<Announcement>();

        [TempData]
        public string? StatusMessage { get; set; }

        private const string HiddenSessionKey = "HiddenAnnouncementIds";

        public async Task<IActionResult> OnGetAsync()
        {
            var hiddenIds = GetHiddenAnnouncementIds();

            // Matches your Announcement.cs model properties: IsActive, CreatedAt
            Announcements = await _context.Announcements
                .Where(a => a.IsActive && !hiddenIds.Contains(a.AnnouncementId))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Page();
        }

        public IActionResult OnPostHide(int id)
        {
            var hiddenIds = GetHiddenAnnouncementIds();
            if (!hiddenIds.Contains(id))
            {
                hiddenIds.Add(id);
                SaveHiddenAnnouncementIds(hiddenIds);
            }

            StatusMessage = "Announcement hidden from view.";
            return RedirectToPage();
        }

        private List<int> GetHiddenAnnouncementIds()
        {
            var sessionData = HttpContext.Session.GetString(HiddenSessionKey);
            if (string.IsNullOrEmpty(sessionData))
            {
                return new List<int>();
            }
            return JsonSerializer.Deserialize<List<int>>(sessionData) ?? new List<int>();
        }

        private void SaveHiddenAnnouncementIds(List<int> ids)
        {
            var sessionData = JsonSerializer.Serialize(ids);
            HttpContext.Session.SetString(HiddenSessionKey, sessionData);
        }
    }
}