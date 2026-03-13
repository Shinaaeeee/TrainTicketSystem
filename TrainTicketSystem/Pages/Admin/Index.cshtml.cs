using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Models; // Đảm bảo đúng namespace Models của bạn

namespace TrainTicketSystem.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly TrainTicketDbContext _context;

        public IndexModel(TrainTicketDbContext context)
        {
            _context = context;
        }

        // Khai báo các biến lưu con số thống kê
        public int TotalUsers { get; set; }
        public int TotalTrains { get; set; }
        public int TotalRoutes { get; set; }
        public int TotalSchedules { get; set; }
        public int TotalSeats { get; set; }
        public int TotalTickets { get; set; }

        // Class phụ để chứa dữ liệu bảng Recent Bookings
        public class RecentBookingDto
        {
            public string? CustomerName { get; set; }
            public string? RouteInfo { get; set; }
            public string? TimeAgo { get; set; }
        }

        public List<RecentBookingDto> RecentBookings { get; set; } = new List<RecentBookingDto>();

        public async Task OnGetAsync()
        {
            // 1. Lấy tổng số lượng từ các bảng
            if (_context.Users != null) TotalUsers = await _context.Users.CountAsync();
            if (_context.Trains != null) TotalTrains = await _context.Trains.CountAsync();
            if (_context.Routes != null) TotalRoutes = await _context.Routes.CountAsync();
            if (_context.Schedules != null) TotalSchedules = await _context.Schedules.CountAsync();
            if (_context.Seats != null) TotalSeats = await _context.Seats.CountAsync();
            if (_context.Bookings != null) TotalTickets = await _context.Bookings.CountAsync();

            // 2. Lấy danh sách 4 lượt đặt vé mới nhất (Join các bảng lại với nhau)
            if (_context.Bookings != null)
            {
                var latestBookings = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Schedule)
                        .ThenInclude(s => s!.Route)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(4)
                    .ToListAsync();

                foreach (var b in latestBookings)
                {
                    // Tính toán hiển thị "vài giờ trước"
                    var timeSpan = DateTime.Now - (b.BookingDate ?? DateTime.Now);
                    string timeStr = timeSpan.TotalHours < 1
                        ? $"{(int)timeSpan.TotalMinutes} mins ago"
                        : $"{(int)timeSpan.TotalHours} hours ago";

                    RecentBookings.Add(new RecentBookingDto
                    {
                        CustomerName = b.User?.FullName ?? "Unknown User",
                        RouteInfo = b.Schedule?.Route != null ? $"{b.Schedule.Route.StartStation} - {b.Schedule.Route.EndStation}" : "Unknown Route",
                        TimeAgo = timeStr
                    });
                }
            }
        }
    }
}