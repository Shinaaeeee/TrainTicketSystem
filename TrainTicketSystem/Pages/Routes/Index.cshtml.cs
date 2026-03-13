using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.Routes
{
    public class IndexModel : PageModel
    {
        private readonly TrainTicketDbContext _context;

        public IndexModel(TrainTicketDbContext context)
        {
            _context = context;
        }

        public IList<Models.Route> RouteList { get; set; } = default!;

        [BindProperty]
        public Models.Route CurrentRoute { get; set; } = default!;

        // Biến hứng từ khóa tìm kiếm (Tên ga đi hoặc ga đến)
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        // ==============================================
        // CÁC BIẾN PHỤC VỤ PHÂN TRANG
        // ==============================================
        [BindProperty(Name = "p", SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 5; // Số dòng trên 1 trang

        // ==============================================
        // 1. HÀM LOAD DỮ LIỆU, TÌM KIẾM & PHÂN TRANG
        // ==============================================
        public async Task OnGetAsync()
        {
            if (_context.Routes != null)
            {
                var query = _context.Routes.AsQueryable();

                // Lọc theo Ga đi hoặc Ga đến nếu có từ khóa
                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    query = query.Where(r =>
                        (r.StartStation != null && r.StartStation.Contains(SearchTerm)) ||
                        (r.EndStation != null && r.EndStation.Contains(SearchTerm)));
                }

                // Tính toán phân trang
                TotalItems = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                // Lấy dữ liệu theo trang
                if (TotalItems > 0)
                {
                    RouteList = await query
                        .Skip((CurrentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToListAsync();
                }
                else
                {
                    RouteList = new List<Models.Route>();
                }
            }
        }

        // ==============================================
        // 2. HÀM XỬ LÝ THÊM MỚI (CREATE)
        // ==============================================
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Routes.Add(CurrentRoute);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        // ==============================================
        // 3. HÀM XỬ LÝ CẬP NHẬT (EDIT)
        // ==============================================
        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Attach(CurrentRoute).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        // ==============================================
        // 4. HÀM XỬ LÝ XÓA (DELETE)
        // ==============================================
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route != null)
            {
                _context.Routes.Remove(route);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }
    }
}