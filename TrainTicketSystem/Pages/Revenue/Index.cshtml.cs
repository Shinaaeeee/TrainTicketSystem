using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.Admin.Revenue
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;

        public IndexModel(IConfiguration config)
        {
            _config = config;
        }

        public RevenueViewModel Revenue { get; set; } = new();

        // Date range filter, bound from query string
        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        public async Task OnGetAsync()
        {
            var connStr = _config.GetConnectionString("MyCnn");

            // Default: if no date range provided, show all time
            // If only ToDate is set, include the full day (23:59:59)
            var from = FromDate;
            var to   = ToDate.HasValue ? ToDate.Value.Date.AddDays(1).AddSeconds(-1) : (DateTime?)null;

            Revenue.FromDate = FromDate;
            Revenue.ToDate   = ToDate;

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            // ---- Query 1: summary stats ----
            // Count bookings by status and total revenue from paid payments
            var summarySql = @"
                SELECT
                    COUNT(*)                                       AS TotalBookings,
                    SUM(CASE WHEN Status = 'Paid'      THEN 1 ELSE 0 END) AS PaidCount,
                    SUM(CASE WHEN Status = 'Pending'   THEN 1 ELSE 0 END) AS PendingCount,
                    ISNULL((
                        SELECT SUM(p.Amount)
                        FROM Payment p
                        WHERE (@From IS NULL OR p.PaymentDate >= @From)
                          AND (@To   IS NULL OR p.PaymentDate <= @To)
                    ), 0) AS TotalRevenue
                FROM Booking b
                WHERE (@From IS NULL OR b.BookingDate >= @From)
                  AND (@To   IS NULL OR b.BookingDate <= @To)";

            using var summaryCmd = new SqlCommand(summarySql, conn);
            summaryCmd.Parameters.AddWithValue("@From", (object?)from ?? DBNull.Value);
            summaryCmd.Parameters.AddWithValue("@To",   (object?)to   ?? DBNull.Value);

            using var summaryReader = await summaryCmd.ExecuteReaderAsync();
            if (await summaryReader.ReadAsync())
            {
                Revenue.TotalBookings  = summaryReader.GetInt32(0);
                Revenue.PaidBookings   = summaryReader.GetInt32(1);
                Revenue.PendingBookings = summaryReader.GetInt32(2);
                Revenue.TotalRevenue   = summaryReader.GetDecimal(3);
            }
            await summaryReader.CloseAsync();

            // ---- Query 2: revenue grouped by route ----
            var routeSql = @"
                SELECT
                    r.StartStation,
                    r.EndStation,
                    COUNT(DISTINCT b.BookingId) AS TotalBookings,
                    ISNULL(SUM(p.Amount), 0)    AS TotalRevenue
                FROM Booking b
                JOIN Schedule sc ON b.ScheduleId = sc.ScheduleId
                JOIN Route r     ON sc.RouteId   = r.RouteId
                -- LEFT JOIN: include routes even if no payment yet
                LEFT JOIN Payment p ON p.BookingId = b.BookingId
                WHERE (@From IS NULL OR b.BookingDate >= @From)
                  AND (@To   IS NULL OR b.BookingDate <= @To)
                GROUP BY r.RouteId, r.StartStation, r.EndStation
                ORDER BY TotalRevenue DESC";

            using var routeCmd = new SqlCommand(routeSql, conn);
            routeCmd.Parameters.AddWithValue("@From", (object?)from ?? DBNull.Value);
            routeCmd.Parameters.AddWithValue("@To",   (object?)to   ?? DBNull.Value);

            using var routeReader = await routeCmd.ExecuteReaderAsync();
            while (await routeReader.ReadAsync())
            {
                Revenue.ByRoute.Add(new RevenueByRouteViewModel
                {
                    StartStation  = routeReader.GetString(0),
                    EndStation    = routeReader.GetString(1),
                    TotalBookings = routeReader.GetInt32(2),
                    TotalRevenue  = routeReader.GetDecimal(3)
                });
            }
            await routeReader.CloseAsync();

            // ---- Query 3: revenue grouped by month ----
            // Shows trend over time — useful for charts or reports
            var monthSql = @"
                SELECT
                    YEAR(p.PaymentDate)  AS Year,
                    MONTH(p.PaymentDate) AS Month,
                    COUNT(*)             AS TotalPayments,
                    SUM(p.Amount)        AS TotalRevenue
                FROM Payment p
                WHERE (@From IS NULL OR p.PaymentDate >= @From)
                  AND (@To   IS NULL OR p.PaymentDate <= @To)
                GROUP BY YEAR(p.PaymentDate), MONTH(p.PaymentDate)
                ORDER BY Year DESC, Month DESC";

            using var monthCmd = new SqlCommand(monthSql, conn);
            monthCmd.Parameters.AddWithValue("@From", (object?)from ?? DBNull.Value);
            monthCmd.Parameters.AddWithValue("@To",   (object?)to   ?? DBNull.Value);

            using var monthReader = await monthCmd.ExecuteReaderAsync();
            while (await monthReader.ReadAsync())
            {
                Revenue.ByMonth.Add(new RevenueByMonthViewModel
                {
                    Year          = monthReader.GetInt32(0),
                    Month         = monthReader.GetInt32(1),
                    TotalPayments = monthReader.GetInt32(2),
                    TotalRevenue  = monthReader.GetDecimal(3)
                });
            }
        }
    }
}
