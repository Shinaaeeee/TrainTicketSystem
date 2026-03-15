using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrainTicketSystem.Models;
using System.Linq;

namespace TrainTicketSystem.Pages
{
    public class LoginModel : PageModel
    {
        private readonly TrainTicketDbContext _context;

        public LoginModel(TrainTicketDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string Message { get; set; }

        public IActionResult OnPost()
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Username == Username && u.Password == Password);

            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                if (user.Role == "Admin")
                {
                    return RedirectToPage("/Admin/Index");
                }
                else
                {
                    return RedirectToPage("/Index");
                }
            }

            Message = "Username or Password incorrect";
            return Page();
        }
    }
}