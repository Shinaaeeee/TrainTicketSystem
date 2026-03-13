using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.Trains
{
    public class IndexModel : PageModel
    {
        private readonly TrainTicketDbContext _context;

        public IndexModel(TrainTicketDbContext context)
        {
            _context = context;
        }

        public IList<Train> TrainList { get; set; } = default!;

        [BindProperty]
        public Train CurrentTrain { get; set; } = default!;

        // Biến hứng từ khóa tìm kiếm
        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }

        // ==============================================
        // CÁC BIẾN PHỤC VỤ PHÂN TRANG
        // ==============================================
        [BindProperty(Name = "p", SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 5; 

        // ==============================================
        // 1. HÀM LOAD DỮ LIỆU, TÌM KIẾM & PHÂN TRANG
        // ==============================================
        public async Task OnGetAsync()
        {
            if (_context.Trains != null)
            {
                var query = _context.Trains.AsQueryable();

                // Lọc theo Tên Tàu nếu có từ khóa
                if (!string.IsNullOrEmpty(SearchName))
                {
                    query = query.Where(t => t.TrainName != null && t.TrainName.Contains(SearchName));
                }

                // Tính toán phân trang
                TotalItems = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                // Lấy dữ liệu theo trang
                if (TotalItems > 0)
                {
                    TrainList = await query
                        .Skip((CurrentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToListAsync();
                }
                else
                {
                    TrainList = new List<Train>();
                }
            }
        }

        // ==============================================
        // 2. HÀM XỬ LÝ THÊM MỚI (CREATE)
        // ==============================================
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Trains.Add(CurrentTrain);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        // ==============================================
        // 3. HÀM XỬ LÝ CẬP NHẬT (EDIT)
        // ==============================================
        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Attach(CurrentTrain).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        // ==============================================
        // 4. HÀM XỬ LÝ XÓA (DELETE)
        // ==============================================
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var train = await _context.Trains.FindAsync(id);
            if (train != null)
            {
                _context.Trains.Remove(train);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }
    }
}