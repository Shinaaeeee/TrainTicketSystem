using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Hubs;
using TrainTicketSystem.Models;
using BookingModel = TrainTicketSystem.Models.Booking;
using TrainTicketSystem.Services;

namespace TrainTicketSystem.Pages.Payment;

public class VnpayReturnModel : PageModel
{
    private readonly TrainTicketDbContext _context;
    private readonly ISeatService _seatService;
    private readonly VnpayService _vnpay;
    private readonly IHubContext<BookingNotificationHub> _notificationHub;

    public VnpayReturnModel(
        TrainTicketDbContext context,
        ISeatService seatService,
        VnpayService vnpay,
        IHubContext<BookingNotificationHub> notificationHub)
    {
        _context = context;
        _seatService = seatService;
        _vnpay = vnpay;
        _notificationHub = notificationHub;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_vnpay.IsValidSignature(Request))
            return RedirectToPage("/Payment/Fail", new { reason = "invalid_signature" });

        var responseCode = Request.Query["vnp_ResponseCode"].ToString();
        var txnRef       = Request.Query["vnp_TxnRef"].ToString();
        var transactionId = Request.Query["vnp_TransactionNo"].ToString();
        var orderInfo    = Request.Query["vnp_OrderInfo"].ToString();

        if (!int.TryParse(txnRef.Split('_')[0], out int bookingId))
            return RedirectToPage("/Payment/Fail", new { reason = "invalid_txn" });

        var booking = await _context.Bookings
            .Include(b => b.BookingDetails)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null)
            return RedirectToPage("/Payment/Fail", new { reason = "not_found" });

        if (booking.Status == "Paid")
            return RedirectToPage("/Payment/Success", new { bookingId });

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.BookingId == bookingId && p.Status == "Pending");

        if (responseCode == "00")
        {
            booking.Status = "Paid";

            if (payment != null)
            {
                payment.Status              = "Paid";
                payment.PaymentDate         = DateTime.Now;
                payment.VnpayTransactionId  = transactionId;
                payment.VnpayOrderInfo      = orderInfo;
            }

            var seatIds = booking.BookingDetails.Select(d => d.SeatId!.Value).ToList();
            await _seatService.ConfirmBookingAsync(seatIds, booking.ScheduleId!.Value);

            await _context.SaveChangesAsync();

            await SendBookingNotificationAsync(booking);

            HttpContext.Session.Remove("SelectedSeats");
            HttpContext.Session.Remove("PendingBookingId");
            HttpContext.Session.Remove("PendingPaymentId");
            HttpContext.Session.Remove("CheckoutScheduleId");

            return RedirectToPage("/Payment/Success", new { bookingId });
        }
        else
        {
            booking.Status = "Cancelled";

            if (payment != null)
            {
                payment.Status             = "Failed";
                payment.VnpayTransactionId = transactionId;
            }

            var seatIds = booking.BookingDetails.Select(d => d.SeatId!.Value).ToList();
            var userId  = HttpContext.Session.GetInt32("UserId") ?? booking.UserId ?? 0;
            foreach (var seatId in seatIds)
                await _seatService.ReleaseSeatAsync(seatId, userId, booking.ScheduleId!.Value);

            await _context.SaveChangesAsync();

            return RedirectToPage("/Payment/Fail", new { bookingId, code = responseCode });
        }
    }

    private async Task SendBookingNotificationAsync(BookingModel booking)
    {
        try
        {
            var fullBooking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Schedule).ThenInclude(s => s!.Route)
                .FirstOrDefaultAsync(b => b.BookingId == booking.BookingId);

            if (fullBooking == null) return;

            var customerName = fullBooking.User?.FullName ?? fullBooking.User?.Username ?? "Khách";
            var route = fullBooking.Schedule?.Route;
            var routeName = route != null
                ? $"{route.StartStation} → {route.EndStation}"
                : "N/A";
            var time = DateTime.Now.ToString("HH:mm");
            var totalPrice = fullBooking.TotalPrice?.ToString("N0") ?? "0";

            await _notificationHub.Clients.Group("admin").SendAsync("ReceiveBookingNotification", new
            {
                bookingId = fullBooking.BookingId,
                customerName,
                routeName,
                time,
                totalPrice,
                seatCount = fullBooking.BookingDetails?.Count ?? 0
            });
        }
        catch
        {
        }
    }
}
