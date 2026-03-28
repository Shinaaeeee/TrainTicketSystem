using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.UserProfile
{
    public class ProfileModel : PageModel
    {
        private readonly TrainTicketDbContext _context;

        public ProfileModel(TrainTicketDbContext context)
        {
            _context = context;
        }

        // Display-only (loaded every GET)
        public User? UserInfo { get; set; }

        // Bound from the edit form
        [BindProperty]
        public ProfileInput Input { get; set; } = new();

        // Controls whether the edit form is shown
        [BindProperty(SupportsGet = true)]
        public bool Edit { get; set; }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/Login");

            UserInfo = _context.Users.Find(userId);
            if (UserInfo == null) return RedirectToPage("/Login");

            // Pre-fill edit form
            Input.FullName = UserInfo.FullName ?? "";
            Input.Email = UserInfo.Email ?? "";
            Input.Phone = UserInfo.Phone ?? "";

            return Page();
        }

        public IActionResult OnPost()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/Login");

            // Validate phone
            if (!Regex.IsMatch(Input.Phone ?? "", @"^0\d{9}$"))
            {
                ModelState.AddModelError("Input.Phone", "Phone must be 10 digits starting with 0.");
            }

            // Validate email
            if (!Regex.IsMatch(Input.Email ?? "", @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                ModelState.AddModelError("Input.Email", "Invalid email format.");
            }

            if (!ModelState.IsValid)
            {
                // Reload display data so the view section still works
                UserInfo = _context.Users.Find(userId);
                Edit = true;
                return Page();
            }

            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.FullName = Input.FullName;
                user.Email = Input.Email;
                user.Phone = Input.Phone;
                _context.SaveChanges();

                // Update session in case FullName changed
                HttpContext.Session.SetString("Username", user.Username);
            }

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToPage();
        }
    }

    public class ProfileInput
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }
}