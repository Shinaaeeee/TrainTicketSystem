using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Hubs;
using TrainTicketSystem.Models;
using TrainTicketSystem.Services;
using PaymentModel = TrainTicketSystem.Models.Payment;

namespace TrainTicketSystem.Pages.Booking;

public class CheckoutModel : PageModel
{
    private readonly TrainTicketDbContext _context;
    private readonly VnpayService _vnpay;
    private readonly IHubContext<TicketHub> _ticketHub;
    private readonly decimal _defaultBasePrice;

    public CheckoutModel(TrainTicketDbContext context, VnpayService vnpay, IConfiguration config, IHubContext<TicketHub> ticketHub)
    {
        _context = context;
        _vnpay = vnpay;
        _ticketHub = ticketHub;
        _defaultBasePrice = config.GetValue<decimal>("Booking:DefaultBasePrice", 100_000m);
    }

    public Schedule? Schedule { get; set; }
    public List<SeatCheckoutItem> SeatItems { get; set; } = [];
    public decimal TotalPrice { get; set; }

    [BindProperty] public int ScheduleId { get; set; }
    [BindProperty] public List<PassengerInput> Passengers { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int scheduleId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Login");

        var seatsRaw = HttpContext.Session.GetString("SelectedSeats");
        if (string.IsNullOrEmpty(seatsRaw)) return RedirectToPage("/Booking/SeatMap", new { scheduleId });

        ScheduleId = scheduleId;

        Schedule = await _context.Schedules
            .Include(s => s.Route)
            .Include(s => s.Train)
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

        if (Schedule == null) return NotFound();

        var seatIds = seatsRaw.Split(',').Select(int.Parse).ToList();

        var rawSeats = await _context.Seats
            .Include(s => s.SeatType)
            .Where(s => seatIds.Contains(s.SeatId))
            .ToListAsync();

        decimal basePrice = Schedule?.Price ?? _defaultBasePrice;
        SeatItems = rawSeats.Select(s => new SeatCheckoutItem
        {
            SeatId     = s.SeatId,
            SeatNumber = s.SeatNumber ?? "",
            SeatType   = s.SeatType?.TypeName ?? "Thường",
            Price      = basePrice * (s.SeatType?.PriceMultiplier ?? 1m)
        }).ToList();

        TotalPrice = SeatItems.Sum(x => x.Price);

        Passengers = SeatItems.Select(s => new PassengerInput { SeatId = s.SeatId }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostPayAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Login");

        if (!ModelState.IsValid)
        {
            await LoadScheduleAndSeats();
            return Page();
        }

        var seatIds = Passengers.Select(p => p.SeatId).ToList();
        var seats = await _context.Seats
            .Include(s => s.SeatType)
            .Where(s => seatIds.Contains(s.SeatId))
            .ToListAsync();

        var schedule = await _context.Schedules.FindAsync(ScheduleId);
        decimal basePrice = schedule?.Price ?? _defaultBasePrice;
        decimal total = seats.Sum(s => basePrice * (s.SeatType?.PriceMultiplier ?? 1m));

        var booking = new Models.Booking
        {
            UserId      = userId,
            ScheduleId  = ScheduleId,
            BookingDate = DateTime.Now,
            TotalPrice  = total,
            Status      = "Pending"
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        foreach (var passenger in Passengers)
        {
            var seat = seats.FirstOrDefault(s => s.SeatId == passenger.SeatId);
            if (seat == null) continue;

            _context.BookingDetails.Add(new BookingDetail
            {
                BookingId      = booking.BookingId,
                SeatId         = passenger.SeatId,
                Price          = basePrice * (seat.SeatType?.PriceMultiplier ?? 1m),
                PassengerName  = passenger.Name,
                PassengerPhone = passenger.Phone
            });
        }

        var payment = new PaymentModel
        {
            BookingId     = booking.BookingId,
            Amount        = total,
            PaymentMethod = "VNPay",
            Status        = "Pending",
            VnpayOrderInfo = $"Dat ve tau - BookingId {booking.BookingId}"
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        await _ticketHub.Clients.Group("tickets").SendAsync("TicketChanged", "create", booking.BookingId);

        HttpContext.Session.SetInt32("PendingBookingId", booking.BookingId);
        HttpContext.Session.SetInt32("PendingPaymentId", payment.PaymentId);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var paymentUrl = _vnpay.BuildPaymentUrl(
            booking.BookingId,
            total,
            payment.VnpayOrderInfo,
            ipAddress
        );

        return Redirect(paymentUrl);
    }

    private async Task LoadScheduleAndSeats()
    {
        Schedule = await _context.Schedules
            .Include(s => s.Route)
            .Include(s => s.Train)
            .FirstOrDefaultAsync(s => s.ScheduleId == ScheduleId);

        var seatIds = Passengers.Select(p => p.SeatId).ToList();
        decimal basePrice = Schedule?.Price ?? _defaultBasePrice;
        var rawSeats = await _context.Seats
            .Include(s => s.SeatType)
            .Where(s => seatIds.Contains(s.SeatId))
            .ToListAsync();

        SeatItems = rawSeats.Select(s => new SeatCheckoutItem
        {
            SeatId     = s.SeatId,
            SeatNumber = s.SeatNumber ?? "",
            SeatType   = s.SeatType?.TypeName ?? "Thường",
            Price      = basePrice * (s.SeatType?.PriceMultiplier ?? 1m)
        }).ToList();

        TotalPrice = SeatItems.Sum(x => x.Price);
    }
}

public class SeatCheckoutItem
{
    public int SeatId { get; set; }
    public string SeatNumber { get; set; } = "";
    public string SeatType { get; set; } = "";
    public decimal Price { get; set; }
}

public class PassengerInput
{
    public int SeatId { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập tên hành khách")]
    [System.ComponentModel.DataAnnotations.MaxLength(100)]
    public string Name { get; set; } = "";

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [System.ComponentModel.DataAnnotations.MaxLength(20)]
    public string Phone { get; set; } = "";
}
