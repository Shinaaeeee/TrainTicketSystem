using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Models;
using TrainTicketSystem.Services;
using PaymentModel = TrainTicketSystem.Models.Payment;

namespace TrainTicketSystem.Pages.Booking;

public class CheckoutModel : PageModel
{
    private readonly TrainTicketDbContext _context;
    private readonly VnpayService _vnpay;
    private readonly decimal _defaultBasePrice;

    public CheckoutModel(TrainTicketDbContext context, VnpayService vnpay, IConfiguration config)
    {
        _context = context;
        _vnpay = vnpay;
        _defaultBasePrice = config.GetValue<decimal>("Booking:DefaultBasePrice", 100_000m);
    }

    // ---- Display data ----
    public Schedule? Schedule { get; set; }
    public List<SeatCheckoutItem> SeatItems { get; set; } = [];
    public decimal TotalPrice { get; set; }

    // ---- Bound form data ----
    [BindProperty] public int ScheduleId { get; set; }
    [BindProperty] public List<PassengerInput> Passengers { get; set; } = [];

    // ------------------------------------------------------------------ //
    //  GET: /Booking/Checkout?scheduleId=1                                //
    // ------------------------------------------------------------------ //
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

        // Load raw data first, compute price in C# (not in LINQ-to-SQL) to avoid decimal precision issues
        var rawSeats = await _context.Seats
            .Include(s => s.SeatType)
            .Where(s => seatIds.Contains(s.SeatId))
            .ToListAsync();

        decimal basePrice = Schedule?.Price ?? _defaultBasePrice; // lấy giá base từ Schedule
        SeatItems = rawSeats.Select(s => new SeatCheckoutItem
        {
            SeatId     = s.SeatId,
            SeatNumber = s.SeatNumber ?? "",
            SeatType   = s.SeatType?.TypeName ?? "Thường",
            Price      = basePrice * (s.SeatType?.PriceMultiplier ?? 1m)
        }).ToList();

        TotalPrice = SeatItems.Sum(x => x.Price);

        // Pre-fill passenger list slots from session
        Passengers = SeatItems.Select(s => new PassengerInput { SeatId = s.SeatId }).ToList();

        return Page();
    }

    // ------------------------------------------------------------------ //
    //  POST: Create booking → redirect to VNPay sandbox                  //
    // ------------------------------------------------------------------ //
    public async Task<IActionResult> OnPostPayAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Login");

        if (!ModelState.IsValid)
        {
            await LoadScheduleAndSeats();
            return Page();
        }

        // 1. Calculate total
        var seatIds = Passengers.Select(p => p.SeatId).ToList();
        var seats = await _context.Seats
            .Include(s => s.SeatType)
            .Where(s => seatIds.Contains(s.SeatId))
            .ToListAsync();

        var schedule = await _context.Schedules.FindAsync(ScheduleId);
        decimal basePrice = schedule?.Price ?? _defaultBasePrice;
        decimal total = seats.Sum(s => basePrice * (s.SeatType?.PriceMultiplier ?? 1m));

        // 2. Create Booking with Pending status
        var booking = new Models.Booking
        {
            UserId      = userId,
            ScheduleId  = ScheduleId,
            BookingDate = DateTime.Now,
            TotalPrice  = total,
            Status      = "Pending"
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();  // need BookingId before details

        // 3. Create BookingDetails (one per passenger/seat)
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

        // 4. Create Payment record (Pending)
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

        // 5. Store bookingId in session for callback retrieval
        HttpContext.Session.SetInt32("PendingBookingId", booking.BookingId);
        HttpContext.Session.SetInt32("PendingPaymentId", payment.PaymentId);

        // 6. Build VNPay URL and redirect
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var paymentUrl = _vnpay.BuildPaymentUrl(
            booking.BookingId,
            total,
            payment.VnpayOrderInfo,
            ipAddress
        );

        return Redirect(paymentUrl);
    }

    // ------------------------------------------------------------------ //
    //  Helpers                                                            //
    // ------------------------------------------------------------------ //
    private async Task LoadScheduleAndSeats()
    {
        Schedule = await _context.Schedules
            .Include(s => s.Route)
            .Include(s => s.Train)
            .FirstOrDefaultAsync(s => s.ScheduleId == ScheduleId);

        var seatIds = Passengers.Select(p => p.SeatId).ToList();
        decimal basePrice = Schedule?.Price ?? _defaultBasePrice; // lấy giá base từ Schedule
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

// ---- DTOs / ViewModels ----
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
