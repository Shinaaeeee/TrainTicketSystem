using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using TrainTicketSystem.Models;

// ✅ Đổi namespace khớp với vị trí file Pages/Tickets/
namespace TrainTicketSystem.Pages.Tickets
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;

        public IndexModel(IConfiguration config)
        {
            _config = config;
        }

        public List<BookingViewModel> Bookings { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }

        public async Task OnGetAsync()
        {
            var connStr = _config.GetConnectionString("MyCnn");

            var sql = @"
                SELECT 
                    b.BookingId,
                    u.FullName,
                    r.StartStation,
                    r.EndStation,
                    t.TrainName,
                    sc.DepartureTime,
                    b.BookingDate,
                    b.TotalPrice,
                    b.Status
                FROM Booking b
                JOIN Users u     ON b.UserId     = u.UserId
                JOIN Schedule sc ON b.ScheduleId = sc.ScheduleId
                JOIN Route r     ON sc.RouteId   = r.RouteId
                JOIN Train t     ON sc.TrainId   = t.TrainId
                WHERE (@Status IS NULL OR b.Status = @Status)
                  AND (@SearchName IS NULL OR u.FullName LIKE '%' + @SearchName + '%')
                ORDER BY b.BookingDate DESC";

            using var conn = new SqlConnection(connStr);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@Status", (object?)StatusFilter ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SearchName", (object?)SearchName ?? DBNull.Value);

            await conn.OpenAsync();
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

            return RedirectToPage();
        }
    }
}