using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BookingModel = TrainTicketSystem.Models.Booking;
using PaymentModel = TrainTicketSystem.Models.Payment;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.Payment;

public class SuccessModel : PageModel
{
    private readonly TrainTicketDbContext _context;
    public SuccessModel(TrainTicketDbContext context) => _context = context;

    public BookingModel? Booking { get; set; }
    public List<BookingDetail> Details { get; set; } = [];
    public PaymentModel? Payment { get; set; }

    public async Task<IActionResult> OnGetAsync(int bookingId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Login");

        Booking = await _context.Bookings
            .Include(b => b.Schedule).ThenInclude(s => s!.Route)
            .Include(b => b.Schedule).ThenInclude(s => s!.Train)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);

        if (Booking == null) return NotFound();

        Details = await _context.BookingDetails
            .Include(d => d.Seat).ThenInclude(s => s!.SeatType)
            .Where(d => d.BookingId == bookingId)
            .ToListAsync();

        Payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.BookingId == bookingId);

        return Page();
    }
}
