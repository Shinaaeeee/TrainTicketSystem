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
        public DateTime? ArrivalDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FromStation { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ToStation { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SeatType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string PriceRange { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; }

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
                            ArrivalTime = s.ArrivalTime,
                            Price = s.Price
                        };

            var now = DateTime.Now;
            query = query.Where(x => x.DepartureTime >= now);

            if (SearchDate != null)
            {
                query = query.Where(x => x.DepartureTime.Value.Date == SearchDate.Value.Date);
            }

            if (ArrivalDate != null)
            {
                query = query.Where(x => x.ArrivalTime.Value.Date == ArrivalDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(FromStation))
            {
                query = query.Where(x => x.FromStation.Contains(FromStation));
            }

            if (!string.IsNullOrEmpty(ToStation))
            {
                query = query.Where(x => x.ToStation.Contains(ToStation));
            }

            if (!string.IsNullOrEmpty(PriceRange))
            {
                switch (PriceRange)
                {
                    case "0-200k":
                        query = query.Where(x => x.Price >= 0 && x.Price <= 200000);
                        break;
                    case "200k-500k":
                        query = query.Where(x => x.Price > 200000 && x.Price <= 500000);
                        break;
                    case "500k-800k":
                        query = query.Where(x => x.Price > 500000 && x.Price <= 800000);
                        break;
                    case "800k-1000k":
                        query = query.Where(x => x.Price > 800000 && x.Price <= 1000000);
                        break;
                }
            }

            query = SortBy switch
            {
                "price_asc" => query.OrderBy(x => x.Price),
                "price_desc" => query.OrderByDescending(x => x.Price),
                "departure_asc" => query.OrderBy(x => x.DepartureTime),
                "departure_desc" => query.OrderByDescending(x => x.DepartureTime),
                "arrival_asc" => query.OrderBy(x => x.ArrivalTime),
                "arrival_desc" => query.OrderByDescending(x => x.ArrivalTime),
                _ => query.OrderBy(x => x.DepartureTime)
            };

            Trains = query.ToList();

            return Page();
        }
    }
}
