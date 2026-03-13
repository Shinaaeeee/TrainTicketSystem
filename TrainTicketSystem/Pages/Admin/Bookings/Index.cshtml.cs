using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.Admin.Bookings
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;

        // Inject IConfiguration to read the connection string from appsettings.json
        public IndexModel(IConfiguration config)
        {
            _config = config;
        }

        // Data bound to the page
        public List<BookingViewModel> Bookings { get; set; } = new();

        // Filter: bound from query string ?StatusFilter=Paid
        // [SupportsGet] lets this property be populated on GET requests
        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }

        public async Task OnGetAsync()
        {
            var connStr = _config.GetConnectionString("MyCnn");

            // Build a flexible query:
            //   - JOIN 4 tables to get all display info in one round-trip
            //   - Use optional WHERE clauses based on filter inputs
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
                JOIN Users u        ON b.UserId     = u.UserId
                JOIN Schedule sc    ON b.ScheduleId = sc.ScheduleId
                JOIN Route r        ON sc.RouteId   = r.RouteId
                JOIN Train t        ON sc.TrainId   = t.TrainId
                WHERE (@Status IS NULL OR b.Status = @Status)
                  AND (@SearchName IS NULL OR u.FullName LIKE '%' + @SearchName + '%')
                ORDER BY b.BookingDate DESC";

            using var conn = new SqlConnection(connStr);
            using var cmd = new SqlCommand(sql, conn);

            // Pass NULL when no filter is selected — SQL handles the rest
            cmd.Parameters.AddWithValue("@Status", (object?)StatusFilter ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SearchName", (object?)SearchName ?? DBNull.Value);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Bookings.Add(new BookingViewModel
                {
                    BookingId     = reader.GetInt32(0),
                    FullName      = reader.GetString(1),
                    StartStation  = reader.GetString(2),
                    EndStation    = reader.GetString(3),
                    TrainName     = reader.GetString(4),
                    DepartureTime = reader.GetDateTime(5),
                    BookingDate   = reader.GetDateTime(6),
                    TotalPrice    = reader.GetDecimal(7),
                    Status        = reader.GetString(8)
                });
            }
        }

        // POST handler: update booking status (Confirm or Cancel)
        // Called from the form buttons on the Index page
        public async Task<IActionResult> OnPostUpdateStatusAsync(int bookingId, string newStatus)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            // Only allow valid status values — never trust user input directly
            var allowed = new[] { "Paid", "Pending", "Cancelled" };
            if (!allowed.Contains(newStatus)) return BadRequest("Trạng thái không hợp lệ.");

            var sql = "UPDATE Booking SET Status = @Status WHERE BookingId = @Id";

            using var conn = new SqlConnection(connStr);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", newStatus);
            cmd.Parameters.AddWithValue("@Id", bookingId);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            // Redirect back to the same page (PRG pattern: Post-Redirect-Get)
            // This prevents duplicate form submission on browser refresh
            return RedirectToPage();
        }
    }
}
