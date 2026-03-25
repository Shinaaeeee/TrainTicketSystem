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
            // USERNAME > 6 ký tự
            if (string.IsNullOrEmpty(User.Username) || User.Username.Length < 6)
            {
                ModelState.AddModelError("User.Username", "Username phải > 6 ký tự");
            }

            // PASSWORD > 6 ký tự + có hoa, thường, số
            if (string.IsNullOrEmpty(User.Password) ||
                User.Password.Length < 6 ||
                !User.Password.Any(char.IsUpper) ||
                !User.Password.Any(char.IsLower) ||
                !User.Password.Any(char.IsDigit))
            {
                ModelState.AddModelError("User.Password", "Password phải >6 ký tự, có chữ hoa, chữ thường và số");
            }

            // PHONE: bắt đầu 03 và đủ 10 số
            if (string.IsNullOrEmpty(User.Phone) ||
                !System.Text.RegularExpressions.Regex.IsMatch(User.Phone, @"^03\d{8}$"))
            {
                ModelState.AddModelError("User.Phone", "Số điện thoại phải bắt đầu bằng 03 và đủ 10 số");
            }

            // EMAIL: phải @gmail.com
            if (string.IsNullOrEmpty(User.Email) ||
                !User.Email.EndsWith("@gmail.com"))
            {
                ModelState.AddModelError("User.Email", "Email phải có định dạng @gmail.com");
            }

            // CHECK USERNAME EXIST
            var existUser = _context.Users
                .FirstOrDefault(u => u.Username == User.Username);

            if (existUser != null)
            {
                ModelState.AddModelError("User.Username", "Username đã tồn tại");
            }

            // ❗ Nếu có lỗi → return luôn
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // OK thì lưu
            User.Role = "Customer";

            _context.Users.Add(User);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Register successful!";

            return RedirectToPage("/Login");
        }
    }
}