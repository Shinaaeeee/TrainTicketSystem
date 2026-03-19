using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Models;
using System.Text;
using ClosedXML.Excel;
using System.IO;

namespace TrainTicketSystem.Pages.MyTickets;

public class IndexModel : PageModel
{
    private readonly TrainTicketDbContext _context;

    public IndexModel(TrainTicketDbContext context)
    {
        _context = context;
    }

    public List<BookingViewModel> Bookings { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Login");

        var list = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Schedule!).ThenInclude(s => s.Route)
            .Include(b => b.Schedule!).ThenInclude(s => s.Train)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        foreach (var b in list)
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

        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Login");

        var list = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Schedule!).ThenInclude(s => s.Route)
            .Include(b => b.Schedule!).ThenInclude(s => s.Train)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        // Build XLSX using ClosedXML
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("MyBookings");

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

        // Format header
        var headerRange = ws.Range(1, 1, 1, 9);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Adjust column widths
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MyBookings.xlsx");
    }

    public async Task<IActionResult> OnPostCancelAsync(int bookingId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Login");

        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null) return NotFound();
        if (booking.UserId != userId) return Forbid();

        // Only allow canceling if not already cancelled
        if (string.Equals(booking.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return RedirectToPage();

        booking.Status = "Cancelled";
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }
}
