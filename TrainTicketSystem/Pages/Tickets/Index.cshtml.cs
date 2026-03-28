using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using TrainTicketSystem.Hubs;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.Tickets
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly IHubContext<TicketHub> _hubContext;

        public IndexModel(IConfiguration config, IHubContext<TicketHub> hubContext)
        {
            _config = config;
            _hubContext = hubContext;
        }

        public List<BookingViewModel> Bookings { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }
        // --- THÊM CODE PHÂN TRANG Ở ĐÂY ---
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }

        public async Task OnGetAsync()
        {
            int pageSize = 5; // Số lượng booking trên mỗi trang (bạn có thể đổi)
            if (PageIndex < 1) PageIndex = 1;

            var connStr = _config.GetConnectionString("MyCnn");
            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            // 1. ĐẾM TỔNG SỐ BẢN GHI ĐỂ TÍNH SỐ TRANG
            var countSql = @"
                SELECT COUNT(*) 
                FROM Booking b
                JOIN Users u ON b.UserId = u.UserId
                WHERE (@Status IS NULL OR b.Status = @Status)
                  AND (@SearchName IS NULL OR u.FullName LIKE '%' + @SearchName + '%')";

            using var countCmd = new SqlCommand(countSql, conn);
            countCmd.Parameters.AddWithValue("@Status", (object?)StatusFilter ?? DBNull.Value);
            countCmd.Parameters.AddWithValue("@SearchName", (object?)SearchName ?? DBNull.Value);

            TotalItems = (int)await countCmd.ExecuteScalarAsync();
            TotalPages = (int)Math.Ceiling(TotalItems / (double)pageSize);

            // 2. LẤY DỮ LIỆU CỦA TRANG HIỆN TẠI
            var sql = @"
                SELECT 
                    b.BookingId, u.FullName, r.StartStation, r.EndStation,
                    t.TrainName, sc.DepartureTime, b.BookingDate, b.TotalPrice, b.Status
                FROM Booking b
                JOIN Users u     ON b.UserId     = u.UserId
                JOIN Schedule sc ON b.ScheduleId = sc.ScheduleId
                JOIN Route r     ON sc.RouteId   = r.RouteId
                JOIN Train t     ON sc.TrainId   = t.TrainId
                WHERE (@Status IS NULL OR b.Status = @Status)
                  AND (@SearchName IS NULL OR u.FullName LIKE '%' + @SearchName + '%')
                ORDER BY b.BookingDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"; 

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", (object?)StatusFilter ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SearchName", (object?)SearchName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Offset", (PageIndex - 1) * pageSize);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Bookings.Add(new BookingViewModel
                {
                    BookingId = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    StartStation = reader.GetString(2),
                    EndStation = reader.GetString(3),
                    TrainName = reader.GetString(4),
                    DepartureTime = reader.GetDateTime(5),
                    BookingDate = reader.GetDateTime(6),
                    TotalPrice = reader.GetDecimal(7),
                    Status = reader.GetString(8)
                });
            }
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int bookingId, string newStatus)
        {
            var connStr = _config.GetConnectionString("MyCnn");

            var allowed = new[] { "Paid", "Cancelled" };
            if (!allowed.Contains(newStatus))
                return BadRequest("Invalid status value.");

            var sql = "UPDATE Booking SET Status = @Status WHERE BookingId = @Id";

            using var conn = new SqlConnection(connStr);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", newStatus);
            cmd.Parameters.AddWithValue("@Id", bookingId);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            await _hubContext.Clients.Group("tickets").SendAsync("TicketChanged", newStatus, bookingId);

            return RedirectToPage();
        }
    }
}
