using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TrainTicketSystem.Pages
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Xóa toàn bộ thông tin Session hiện tại
            HttpContext.Session.Clear();

            // Chuyển hướng người dùng về trang Login
            return RedirectToPage("/Login");
        }
    }
}