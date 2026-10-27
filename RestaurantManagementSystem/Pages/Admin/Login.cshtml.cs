using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RestaurantManagementSystem.Pages.Admin
{
    [IgnoreAntiforgeryToken]
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // Hardcoded admin credentials
            if (Username == "admin" && Password == "Admin@123")
            {
                // Store login information in Session
                HttpContext.Session.SetString(
                    "AdminLoggedIn",
                    "true"
                );

                // Go to Dashboard
                return RedirectToPage("/Admin/Dashboard");
            }

            ModelState.AddModelError(
                string.Empty,
                "Invalid username or password."
            );

            return Page();
        }
    }
}