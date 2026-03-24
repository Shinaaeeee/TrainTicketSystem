using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrainTicketSystem.Models;


namespace TrainTicketSystem.Pages.Trains
{
    public class TrainDetailModel : PageModel
    {
        private readonly TrainTicketDbContext _context;

        public TrainDetailModel(TrainTicketDbContext context)
        {
            _context = context;
        }

        public Schedule Schedule { get; set; }
        public Train Train { get; set; }
        public Models.Route Route { get; set; }

        public List<Seat> Seats { get; set; }

        public List<int> BookedSeatIds { get; set; }

        [BindProperty]
        public List<int> SelectedSeats { get; set; }

        public void OnGet(int id)
        {
            LoadData(id);
        }

        public IActionResult OnPostBook(int scheduleId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["Message"] = "⚠ Please login to book ticket";
                return RedirectToPage("/Login");
            }

            if (SelectedSeats == null || SelectedSeats.Count == 0)
            {
                return RedirectToPage(new { id = scheduleId });
            }

            var booking = new TrainTicketSystem.Models.Booking
            {
                UserId = userId,
                BookingDate = DateTime.Now,
                Status = "Booked"
            };

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            foreach (var seatId in SelectedSeats)
            {
                BookingDetail detail = new BookingDetail
                {
                    BookingId = booking.BookingId,
                    SeatId = seatId
                };

                _context.BookingDetails.Add(detail);
            }

            _context.SaveChanges();

            return RedirectToPage("/MyTickets");
        }

        private void LoadData(int id)
        {
            Schedule = _context.Schedules.FirstOrDefault(x => x.ScheduleId == id);

            if (Schedule != null)
            {
                Train = _context.Trains.FirstOrDefault(x => x.TrainId == Schedule.TrainId);
                Route = _context.Routes.FirstOrDefault(x => x.RouteId == Schedule.RouteId);

                Seats = _context.Seats
                        .Where(x => x.TrainId == Train.TrainId)
                        .ToList();

                BookedSeatIds = _context.BookingDetails
                        .Where(x => x.SeatId != null)
                        .Select(x => x.SeatId.Value)
                        .ToList();
            }
        }
    }
}