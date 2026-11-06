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
        private readonly IWebHostEnvironment _environment;

        public AnnouncementsModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

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

            // 1. IMAGE FILE VALIDATION CONSTRAINTS (Max 15MB)
            if (ImageFile != null && ImageFile.Length > 0)
            {
                if (!ImageFile.ContentType.StartsWith("image/"))
                {
                    ModelState.AddModelError("ImageFile", "Please upload a valid image file (e.g., JPG, PNG, WEBP).");
                }

                long maxFileSize = 15 * 1024 * 1024; // 15 MB
                if (ImageFile.Length > maxFileSize)
                {
                    ModelState.AddModelError("ImageFile", "The image file is too large. Maximum allowed size is 15MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync();
                return Page();
            }

            string cleanTitle = Title.Trim();
            string cleanMessage = Message.Trim();
            string? imagePath = null;

            // 2. PROCESS & SAVE IMAGE FILE SAFELY
            if (ImageFile != null && ImageFile.Length > 0)
            {
                string webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string uploadsFolder = Path.Combine(webRootPath, "images", "announcements");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(fileStream);
                }

                imagePath = "/images/announcements/" + uniqueFileName;
            }

            // 3. Save the new Announcement
            var announcement = new Announcement
            {
                Title = cleanTitle,
                Message = cleanMessage,
                ImagePath = imagePath,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            // 4. Fetch all registered customer IDs
            var customerIds = await _context.Customers
                .Select(c => c.CustomerId)
                .ToListAsync();

            // 5. Send notification to every customer
            foreach (var customerId in customerIds)
            {
                await _context.AddNotificationAsync(
                    customerId,
                    $"📢 Announcement: {cleanTitle}"
                );
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Announcement posted and notification sent to all customers.";
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