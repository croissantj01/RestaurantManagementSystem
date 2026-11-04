using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Pages.Admin
{
    public class AnnouncementsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AnnouncementsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Announcement> AnnouncementList { get; set; } = new();

        [BindProperty]
        [Required(ErrorMessage = "Please enter a title.")]
        [StringLength(100, ErrorMessage = "Title can be up to 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please enter a message.")]
        [StringLength(1000, ErrorMessage = "Message can be up to 1000 characters.")]
        public string Message { get; set; } = string.Empty;

        private bool IsAdmin =>
            HttpContext.Session.GetString("AdminLoggedIn") == "true";

        private async Task LoadAsync()
        {
            AnnouncementList = await _context.Announcements
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAdmin) return RedirectToPage("/Admin/Login");

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!IsAdmin) return RedirectToPage("/Admin/Login");

            if (!ModelState.IsValid)
            {
                await LoadAsync();
                return Page();
            }

            _context.Announcements.Add(new Announcement
            {
                Title = Title.Trim(),
                Message = Message.Trim(),
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Announcement posted.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleAsync(int id)
        {
            if (!IsAdmin) return RedirectToPage("/Admin/Login");

            var item = await _context.Announcements.FindAsync(id);
            if (item != null)
            {
                item.IsActive = !item.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = item.IsActive
                    ? "Announcement is now visible."
                    : "Announcement hidden.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!IsAdmin) return RedirectToPage("/Admin/Login");

            var item = await _context.Announcements.FindAsync(id);
            if (item != null)
            {
                _context.Announcements.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Announcement deleted.";
            }

            return RedirectToPage();
        }
    }
}