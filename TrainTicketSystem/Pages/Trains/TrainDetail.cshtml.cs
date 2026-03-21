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

        // Thêm dấu ? để biểu thị các đối tượng này có thể null
        public Schedule? Schedule { get; set; }
        public Train? Train { get; set; }
        public Models.Route? Route { get; set; }

        // Khởi tạo sẵn danh sách rỗng để tránh cảnh báo CS8618
        public List<Seat> Seats { get; set; } = new List<Seat>();
        public List<int> BookedSeatIds { get; set; } = new List<int>();

        [BindProperty]
        public List<int> SelectedSeats { get; set; } = new List<int>();

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

            // Sửa lỗi CS0118: Chỉ định rõ đây là Models.Booking để không bị nhầm với namespace
            var booking = new Models.Booking
            {
                UserId = userId, // userId lúc này chắc chắn có giá trị
                BookingDate = DateTime.Now,
                Status = "Booked"
            };

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            foreach (var seatId in SelectedSeats)
            {
                // Chỉ định rõ Models.BookingDetail cho an toàn
                Models.BookingDetail detail = new Models.BookingDetail
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

                // Thêm kiểm tra Train != null để tránh lỗi CS8602 (Dereference of a possibly null reference)
                if (Train != null)
                {
                    Seats = _context.Seats
                            .Where(x => x.TrainId == Train.TrainId)
                            .ToList();
                }

                // Chọn ra những ghế đã được đặt
                BookedSeatIds = _context.BookingDetails
                        .Where(x => x.SeatId != null)
                        .Select(x => x.SeatId!.Value) // Thêm dấu ! để khẳng định SeatId không null ở bước này
                        .ToList();
            }
        }
    }
}