using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Models;
using ClosedXML.Excel;
using System.IO;

namespace TrainTicketSystem.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly TrainTicketDbContext _context;

        public IndexModel(TrainTicketDbContext context)
        {
            _context = context;
        }

        // Statistics
        public int TotalUsers { get; set; }
        public int TotalTrains { get; set; }
        public int TotalRoutes { get; set; }
        public int TotalSchedules { get; set; }
        public int TotalSeats { get; set; }
        public int TotalTickets { get; set; }

        public class RecentBookingDto
        {
            public string? CustomerName { get; set; }
            public string? RouteInfo { get; set; }
            public string? TimeAgo { get; set; }
        }

        public List<RecentBookingDto> RecentBookings { get; set; } = new();

        // Bookings listing for admin
        public List<BookingViewModel> Bookings { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role) || !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToPage("/Login");
            }

            if (_context.Users != null) TotalUsers = await _context.Users.CountAsync();
            if (_context.Trains != null) TotalTrains = await _context.Trains.CountAsync();
            if (_context.Routes != null) TotalRoutes = await _context.Routes.CountAsync();
            if (_context.Schedules != null) TotalSchedules = await _context.Schedules.CountAsync();
            if (_context.Seats != null) TotalSeats = await _context.Seats.CountAsync();
            if (_context.Bookings != null) TotalTickets = await _context.Bookings.CountAsync();

            if (_context.Bookings != null)
            {
                var latestBookings = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Schedule).ThenInclude(s => s!.Route)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(4)
                    .ToListAsync();

                foreach (var b in latestBookings)
                {
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

            // Load all bookings for admin list
            if (_context.Bookings != null)
            {
                var all = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Schedule).ThenInclude(s => s!.Route)
                    .Include(b => b.Schedule).ThenInclude(s => s.Train)
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();

                foreach (var b in all)
                {
                    Bookings.Add(new BookingViewModel
                    {
                        BookingId = b.BookingId,
                        FullName = b.User?.FullName ?? "",
                        StartStation = b.Schedule?.Route?.StartStation ?? "",
                        EndStation = b.Schedule?.Route?.EndStation ?? "",
                        TrainName = b.Schedule?.Train?.TrainName ?? "",
                        DepartureTime = b.Schedule?.DepartureTime ?? DateTime.MinValue,
                        BookingDate = b.BookingDate ?? DateTime.MinValue,
                        TotalPrice = b.TotalPrice ?? 0m,
                        Status = b.Status ?? ""
                    });
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            var role = HttpContext.Session.GetString("Role") ?? string.Empty;
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var list = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Schedule!).ThenInclude(s => s.Route)
                .Include(b => b.Schedule!).ThenInclude(s => s.Train)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("AllBookings");

            // Header
            ws.Cell(1, 1).Value = "BookingId";
            ws.Cell(1, 2).Value = "FullName";
            ws.Cell(1, 3).Value = "StartStation";
            ws.Cell(1, 4).Value = "EndStation";
            ws.Cell(1, 5).Value = "TrainName";
            ws.Cell(1, 6).Value = "DepartureTime";
            ws.Cell(1, 7).Value = "BookingDate";
            ws.Cell(1, 8).Value = "TotalPrice";
            ws.Cell(1, 9).Value = "Status";

            int r = 2;
            foreach (var b in list)
            {
                ws.Cell(r, 1).Value = b.BookingId;
                ws.Cell(r, 2).Value = b.User?.FullName ?? "";
                ws.Cell(r, 3).Value = b.Schedule?.Route?.StartStation ?? "";
                ws.Cell(r, 4).Value = b.Schedule?.Route?.EndStation ?? "";
                ws.Cell(r, 5).Value = b.Schedule?.Train?.TrainName ?? "";
                ws.Cell(r, 6).Value = b.Schedule?.DepartureTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                ws.Cell(r, 7).Value = b.BookingDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                ws.Cell(r, 8).Value = b.TotalPrice ?? 0m;
                ws.Cell(r, 9).Value = b.Status ?? "";
                r++;
            }

            var headerRange = ws.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AllBookings.xlsx");
        }

        public async Task<IActionResult> OnPostCancelAsync(int bookingId)
        {
            var role = HttpContext.Session.GetString("Role") ?? string.Empty;
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return NotFound();

            if (string.Equals(booking.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                return RedirectToPage();

            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
