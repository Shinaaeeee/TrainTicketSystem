using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.MyTickets;

public class DetailsModel : PageModel
{
    private readonly IConfiguration _config;
    private readonly TrainTicketDbContext _context;

    public DetailsModel(IConfiguration config, TrainTicketDbContext context)
    {
        _config = config;
        _context = context;
    }

    public BookingDetailViewModel? Booking { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Login");

        var connStr = _config.GetConnectionString("MyCnn");
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

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
            LEFT JOIN Payment p ON p.BookingId = b.BookingId
            WHERE b.BookingId = @Id AND b.UserId = @UserId";

        using var headerCmd = new SqlCommand(headerSql, conn);
        headerCmd.Parameters.AddWithValue("@Id", id);
        headerCmd.Parameters.AddWithValue("@UserId", userId.Value);

        using var reader = await headerCmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return RedirectToPage("/MyTickets/Index");
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
            PaymentMethod = reader.IsDBNull(12) ? null : reader.GetString(12),
            PaymentDate   = reader.IsDBNull(13) ? null : reader.GetDateTime(13)
        };

        await reader.CloseAsync();

        var seatSql = @"
            SELECT s.SeatNumber, st.TypeName, bd.Price,
                   bd.PassengerName, bd.PassengerPhone
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
                SeatNumber     = seatReader.GetString(0),
                SeatTypeName   = seatReader.GetString(1),
                Price          = seatReader.GetDecimal(2),
                PassengerName  = seatReader.IsDBNull(3) ? null : seatReader.GetString(3),
                PassengerPhone = seatReader.IsDBNull(4) ? null : seatReader.GetString(4)
            });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Login");

        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null) return NotFound();
        if (booking.UserId != userId) return Forbid();

        if (string.Equals(booking.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return RedirectToPage(new { id });

        booking.Status = "Cancelled";
        await _context.SaveChangesAsync();

        return RedirectToPage(new { id });
    }
}
