using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Hubs;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.Schedules
{
    public class IndexModel : PageModel
    {
        private readonly TrainTicketDbContext _context;
        private readonly IHubContext<ScheduleHub> _hubContext;

        public IndexModel(TrainTicketDbContext context, IHubContext<ScheduleHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public IList<Models.Schedule> ScheduleList { get; set; } = default!;

        [BindProperty]
        public Models.Schedule CurrentSchedule { get; set; } = default!;

        public SelectList TrainOptions { get; set; } = default!;
        public SelectList RouteOptions { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(Name = "p", SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 5;

        private async Task LoadDropdownDataAsync()
        {
            var trains = await _context.Trains.ToListAsync();
            TrainOptions = new SelectList(trains, "TrainId", "TrainName");

            var routes = await _context.Routes.ToListAsync();
            var routeDisplayList = routes.Select(r => new
            {
                RouteId = r.RouteId,
                Display = $"{r.StartStation} - {r.EndStation}"
            }).ToList();
            RouteOptions = new SelectList(routeDisplayList, "RouteId", "Display");
        }

        public async Task OnGetAsync()
        {
            if (_context.Schedules != null)
            {
                await LoadDropdownDataAsync();

                var query = _context.Schedules
                    .Include(s => s.Train)
                    .Include(s => s.Route)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    query = query.Where(s =>
                        (s.Train != null && s.Train.TrainName != null && s.Train.TrainName.Contains(SearchTerm)) ||
                        (s.Route != null && s.Route.StartStation != null && s.Route.StartStation.Contains(SearchTerm)) ||
                        (s.Route != null && s.Route.EndStation != null && s.Route.EndStation.Contains(SearchTerm))
                    );
                }

                TotalItems = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                if (TotalItems > 0)
                {
                    ScheduleList = await query
                        .OrderByDescending(s => s.DepartureTime)
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
            if (CurrentSchedule.DepartureTime < DateTime.Now)
            {
                ModelState.AddModelError("CurrentSchedule.DepartureTime", "Thời gian khởi hành không được trong quá khứ.");
            }

            if (CurrentSchedule.ArrivalTime <= CurrentSchedule.DepartureTime)
            {
                ModelState.AddModelError("CurrentSchedule.ArrivalTime", "Thời gian đến phải lớn hơn thời gian khởi hành.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownDataAsync();
                ViewData["ShowCreateModal"] = true;
                return Page();
            }

            _context.Schedules.Add(CurrentSchedule);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group("schedules").SendAsync("ScheduleChanged", "create", CurrentSchedule.ScheduleId);

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownDataAsync();
                return Page();
            }

            _context.Attach(CurrentSchedule).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group("schedules").SendAsync("ScheduleChanged", "edit", CurrentSchedule.ScheduleId);

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group("schedules").SendAsync("ScheduleChanged", "delete", id);
            }
            return RedirectToPage("./Index");
        }
    }
}
