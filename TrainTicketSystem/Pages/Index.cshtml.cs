using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Models;
using TrainTicketSystem.ViewModels;

namespace TrainTicketSystem.Pages
{
    public class IndexModel : PageModel
    {
        private readonly TrainTicketDbContext _context;

        public IndexModel(TrainTicketDbContext context)
        {
            _context = context;
        }

        public List<TrainViewModel> Trains { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? SearchDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FromStation { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ToStation { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SeatType { get; set; }

        public IActionResult OnGet()
        {
            
            var role = HttpContext.Session.GetString("Role");

            if (role == "Admin")
            {
                return RedirectToPage("/Admin/Index");
            }

            var query = from s in _context.Schedules
                        join t in _context.Trains on s.TrainId equals t.TrainId
                        join r in _context.Routes on s.RouteId equals r.RouteId
                        select new TrainViewModel
                        {
                            ScheduleId = s.ScheduleId,
                            TrainName = t.TrainName,
                            FromStation = r.StartStation,
                            ToStation = r.EndStation,
                            DepartureTime = s.DepartureTime,
                            Price = s.Price
                        };
            var now = DateTime.Now;

            query = query.Where(x => x.DepartureTime >= now);
            if (SearchDate != null)
            {
                query = query.Where(x => x.DepartureTime.Value.Date == SearchDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(FromStation))
            {
                query = query.Where(x => x.FromStation.Contains(FromStation));
            }

            if (!string.IsNullOrEmpty(ToStation))
            {
                query = query.Where(x => x.ToStation.Contains(ToStation));
            }

            Trains = query.ToList();

            return Page();
        }
    }
}
