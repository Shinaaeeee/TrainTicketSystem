using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly TrainTicketDbContext _context;

        public RegisterModel(TrainTicketDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User User { get; set; }

        public string Message { get; set; }

        public IActionResult OnPost()
        {
            var existUser = _context.Users
                .FirstOrDefault(u => u.Username == User.Username);

            if (existUser != null)
            {
                Message = "Username already exists";
                return Page();
            }

            User.Role = "Customer";

            _context.Users.Add(User);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Register successful!";

            return RedirectToPage("/Login");
        }
    }
}