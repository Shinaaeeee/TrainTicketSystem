using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        [BindProperty]
        public User User { get; set; }

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("Username");

            if (username == null)
            {
                return RedirectToPage("/Login");
            }

            User = _context.Users.FirstOrDefault(x => x.Username == username);

            return Page();
        }

        public IActionResult OnPost()
        {
            var username = HttpContext.Session.GetString("Username");

            if (username == null)
            {
                return RedirectToPage("/Login");
            }      
            if (!System.Text.RegularExpressions.Regex.IsMatch(User.Phone ?? "", @"^03\d{8}$"))
            {
                ModelState.AddModelError("User.Phone", "SĐT phải có 10 số và bắt đầu bằng 03");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(User.Email ?? "", @"^[a-zA-Z0-9._%+-]+@gmail\.com$"))
            {
                ModelState.AddModelError("User.Email", "Email phải có định dạng @gmail.com");
            }
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = _context.Users.FirstOrDefault(x => x.Username == username);

            if (user != null)
            {
                user.FullName = User.FullName;
                user.Email = User.Email;
                user.Phone = User.Phone;

                _context.SaveChanges();
            }

            ViewData["Message"] = "Cập nhật thành công!";
            return Page();
        }
    }
}