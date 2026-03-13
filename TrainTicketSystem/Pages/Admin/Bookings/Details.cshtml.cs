using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.Admin.Bookings
{
    public class DetailsModel : PageModel
    {
        private readonly IConfiguration _config;

        public DetailsModel(IConfiguration config)
        {
            _config = config;
        }

        public BookingDetailViewModel? Booking { get; set; }

        // id comes from the route: /Admin/Bookings/Details?id=1
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var connStr = _config.GetConnectionString("MyCnn");

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            // ---- Query 1: booking header + customer + payment ----
            var headerSql = @"
                SELECT
                    b.BookingId, u.FullName, u.Email, u.Phone,
                    t.TrainName,
                    r.StartStation, r.EndStation,
                    sc.DepartureTime, sc.ArrivalTime,
                    b.BookingDate, b.TotalPrice, b.Status,
                    p.PaymentMethod, p.PaymentDate
                FROM Booking b
                JOIN Users u     ON b.UserId     = u.UserId
                JOIN Schedule sc ON b.ScheduleId = sc.ScheduleId
                JOIN Route r     ON sc.RouteId   = r.RouteId
                JOIN Train t     ON sc.TrainId   = t.TrainId
                -- LEFT JOIN because a booking may not have a payment yet
                LEFT JOIN Payment p ON p.BookingId = b.BookingId
                WHERE b.BookingId = @Id";

            using var headerCmd = new SqlCommand(headerSql, conn);
            headerCmd.Parameters.AddWithValue("@Id", id);

            using var reader = await headerCmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                // No booking found → return 404
                return NotFound();
            }

            Booking = new BookingDetailViewModel
            {
                BookingId     = reader.GetInt32(0),
                FullName      = reader.GetString(1),
                Email         = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Phone         = reader.IsDBNull(3) ? "" : reader.GetString(3),
                TrainName     = reader.GetString(4),
                StartStation  = reader.GetString(5),
                EndStation    = reader.GetString(6),
                DepartureTime = reader.GetDateTime(7),
                ArrivalTime   = reader.GetDateTime(8),
                BookingDate   = reader.GetDateTime(9),
                TotalPrice    = reader.GetDecimal(10),
                Status        = reader.GetString(11),
                // Payment fields are nullable (LEFT JOIN)
                PaymentMethod = reader.IsDBNull(12) ? null : reader.GetString(12),
                PaymentDate   = reader.IsDBNull(13) ? null : reader.GetDateTime(13)
            };

            // Must close reader before reusing the same connection
            await reader.CloseAsync();

            // ---- Query 2: seat rows for this booking ----
            var seatSql = @"
                SELECT s.SeatNumber, st.TypeName, bd.Price
                FROM BookingDetail bd
                JOIN Seat s     ON bd.SeatId     = s.SeatId
                JOIN SeatType st ON s.SeatTypeId = st.SeatTypeId
                WHERE bd.BookingId = @Id
                ORDER BY s.SeatNumber";

            using var seatCmd = new SqlCommand(seatSql, conn);
            seatCmd.Parameters.AddWithValue("@Id", id);

            using var seatReader = await seatCmd.ExecuteReaderAsync();
            while (await seatReader.ReadAsync())
            {
                Booking.Seats.Add(new SeatRowViewModel
                {
                    SeatNumber   = seatReader.GetString(0),
                    SeatTypeName = seatReader.GetString(1),
                    Price        = seatReader.GetDecimal(2)
                });
            }

            return Page();
        }
    }
}
