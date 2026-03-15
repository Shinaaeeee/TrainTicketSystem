using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.Revenue
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;

        public IndexModel(IConfiguration config)
        {
            _config = config;
        }

        public RevenueViewModel Revenue { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        public async Task OnGetAsync()
        {
            var connStr = _config.GetConnectionString("MyCnn");

            // Normalize: ToDate bao gồm toàn bộ ngày cuối (23:59:59)
            var from = FromDate?.Date;
            var to = ToDate?.Date.AddDays(1).AddTicks(-1);

            Revenue.FromDate = FromDate;
            Revenue.ToDate = ToDate;

            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            // ── Query 1: Summary ─────────────────────────────────────────
            // Dùng LEFT JOIN Payment để TotalRevenue đồng nhất với cùng
            // tập Booking được lọc (tránh mismatch BookingDate vs PaymentDate)
            const string summarySql = @"
                SELECT
                    COUNT(b.BookingId)                                              AS TotalBookings,
                    SUM(CASE WHEN b.Status = 'Paid'    THEN 1 ELSE 0 END)         AS PaidCount,
                    SUM(CASE WHEN b.Status = 'Pending' THEN 1 ELSE 0 END)         AS PendingCount,
                    ISNULL(SUM(CASE WHEN b.Status = 'Paid' THEN p.Amount END), 0) AS TotalRevenue
                FROM Booking b
                LEFT JOIN Payment p ON p.BookingId = b.BookingId
                WHERE (@From IS NULL OR b.BookingDate >= @From)
                  AND (@To   IS NULL OR b.BookingDate <= @To)";

            await using (var cmd = new SqlCommand(summarySql, conn))
            {
                cmd.Parameters.AddWithValue("@From", (object?)from ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@To", (object?)to ?? DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    Revenue.TotalBookings = reader.GetInt32(0);
                    Revenue.PaidBookings = reader.GetInt32(1);
                    Revenue.PendingBookings = reader.GetInt32(2);
                    Revenue.TotalRevenue = reader.GetDecimal(3);
                }
            }

            // ── Query 2: Revenue by Route ─────────────────────────────────
            const string routeSql = @"
                SELECT
                    r.StartStation,
                    r.EndStation,
                    COUNT(DISTINCT b.BookingId)                                AS TotalBookings,
                    ISNULL(SUM(CASE WHEN b.Status = 'Paid' THEN p.Amount END), 0) AS TotalRevenue
                FROM Booking b
                JOIN Schedule sc ON b.ScheduleId = sc.ScheduleId
                JOIN Route    r  ON sc.RouteId   = r.RouteId
                LEFT JOIN Payment p ON p.BookingId = b.BookingId
                WHERE (@From IS NULL OR b.BookingDate >= @From)
                  AND (@To   IS NULL OR b.BookingDate <= @To)
                GROUP BY r.RouteId, r.StartStation, r.EndStation
                ORDER BY TotalRevenue DESC";

            await using (var cmd = new SqlCommand(routeSql, conn))
            {
                cmd.Parameters.AddWithValue("@From", (object?)from ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@To", (object?)to ?? DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Revenue.ByRoute.Add(new RevenueByRouteViewModel
                    {
                        StartStation = reader.GetString(0),
                        EndStation = reader.GetString(1),
                        TotalBookings = reader.GetInt32(2),
                        TotalRevenue = reader.GetDecimal(3)
                    });
                }
            }

            // ── Query 3: Revenue by Month ─────────────────────────────────
            // Dựa trên PaymentDate vì đây là thời điểm tiền thực sự về
            const string monthSql = @"
                SELECT
                    YEAR(p.PaymentDate)  AS Year,
                    MONTH(p.PaymentDate) AS Month,
                    COUNT(*)             AS TotalPayments,
                    SUM(p.Amount)        AS TotalRevenue
                FROM Payment p
                JOIN Booking b ON b.BookingId = p.BookingId
                WHERE (@From IS NULL OR b.BookingDate >= @From)
                  AND (@To   IS NULL OR b.BookingDate <= @To)
                GROUP BY YEAR(p.PaymentDate), MONTH(p.PaymentDate)
                ORDER BY Year DESC, Month DESC";

            await using (var cmd = new SqlCommand(monthSql, conn))
            {
                cmd.Parameters.AddWithValue("@From", (object?)from ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@To", (object?)to ?? DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Revenue.ByMonth.Add(new RevenueByMonthViewModel
                    {
                        Year = reader.GetInt32(0),
                        Month = reader.GetInt32(1),
                        TotalPayments = reader.GetInt32(2),
                        TotalRevenue = reader.GetDecimal(3)
                    });
                }
            }
        }
    }
}