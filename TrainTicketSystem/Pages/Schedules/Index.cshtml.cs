using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.Schedules
{
    public class IndexModel : PageModel
    {
        private readonly TrainTicketDbContext _context;

        public IndexModel(TrainTicketDbContext context)
        {
            _context = context;
        }

        public IList<Models.Schedule> ScheduleList { get; set; } = default!;

        [BindProperty]
        public Models.Schedule CurrentSchedule { get; set; } = default!;

        // 2 Biến này dùng để đổ dữ liệu vào Dropdown (Select list) cho Modal
        public SelectList TrainOptions { get; set; } = default!;
        public SelectList RouteOptions { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        // ==============================================
        // CÁC BIẾN PHỤC VỤ PHÂN TRANG
        // ==============================================
        [BindProperty(Name = "p", SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 5;

        public async Task OnGetAsync()
        {
            if (_context.Schedules != null)
            {
                // 1. Load danh sách Tàu và Tuyến đường cho Dropdown List
                var trains = await _context.Trains.ToListAsync();
                TrainOptions = new SelectList(trains, "TrainId", "TrainName");

                var routes = await _context.Routes.ToListAsync();
                // Ghép nối Ga đi - Ga đến cho dễ nhìn
                var routeDisplayList = routes.Select(r => new
                {
                    RouteId = r.RouteId,
                    Display = $"{r.StartStation} - {r.EndStation}"
                }).ToList();
                RouteOptions = new SelectList(routeDisplayList, "RouteId", "Display");

                // 2. Load danh sách Lịch trình, JOIN (Include) với bảng Train và Route
                var query = _context.Schedules
                    .Include(s => s.Train)
                    .Include(s => s.Route)
                    .AsQueryable();

                // Lọc theo Tên tàu hoặc Ga đi/đến
                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    query = query.Where(s =>
                        (s.Train != null && s.Train.TrainName != null && s.Train.TrainName.Contains(SearchTerm)) ||
                        (s.Route != null && s.Route.StartStation != null && s.Route.StartStation.Contains(SearchTerm)) ||
                        (s.Route != null && s.Route.EndStation != null && s.Route.EndStation.Contains(SearchTerm))
                    );
                }

                // Tính toán phân trang
                TotalItems = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                if (TotalItems > 0)
                {
                    ScheduleList = await query
                        .OrderByDescending(s => s.DepartureTime) // Sắp xếp chuyến mới nhất lên đầu
                        .Skip((CurrentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToListAsync();
                }
                else
                {
                    ScheduleList = new List<Models.Schedule>();
                }
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Schedules.Add(CurrentSchedule);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Attach(CurrentSchedule).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }
    }
}