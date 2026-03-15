using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Models;
using TrainTicketSystem.Services;

namespace TrainTicketSystem.Pages.Payment;

/// <summary>
/// Handles the VNPay IPN/Return callback after payment.
/// VNPay redirects the user browser to this page with result query params.
/// </summary>
public class VnpayReturnModel : PageModel
{
    private readonly TrainTicketDbContext _context;
    private readonly ISeatService _seatService;
    private readonly VnpayService _vnpay;

    public VnpayReturnModel(TrainTicketDbContext context, ISeatService seatService, VnpayService vnpay)
    {
        _context = context;
        _seatService = seatService;
        _vnpay = vnpay;
    }

    // ------------------------------------------------------------------ //
    //  GET: /Payment/VnpayReturn?vnp_ResponseCode=00&...                  //
    // ------------------------------------------------------------------ //
    public async Task<IActionResult> OnGetAsync()
    {
        // 1. Verify HMAC-SHA512 signature — reject tampered responses
        if (!_vnpay.IsValidSignature(Request))
            return RedirectToPage("/Payment/Fail", new { reason = "invalid_signature" });

        var responseCode = Request.Query["vnp_ResponseCode"].ToString();
        var txnRef       = Request.Query["vnp_TxnRef"].ToString();          // "bookingId_timestamp"
        var transactionId = Request.Query["vnp_TransactionNo"].ToString();
        var orderInfo    = Request.Query["vnp_OrderInfo"].ToString();

        // 2. Parse bookingId from TxnRef ("bookingId_timestamp")
        if (!int.TryParse(txnRef.Split('_')[0], out int bookingId))
            return RedirectToPage("/Payment/Fail", new { reason = "invalid_txn" });

        // 3. Load booking
        var booking = await _context.Bookings
            .Include(b => b.BookingDetails)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null)
            return RedirectToPage("/Payment/Fail", new { reason = "not_found" });

        // 4. Idempotency guard — already processed
        if (booking.Status == "Paid")
            return RedirectToPage("/Payment/Success", new { bookingId });

        // 5. Load the pending payment record
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.BookingId == bookingId && p.Status == "Pending");

        if (responseCode == "00")  // "00" = success in VNPay
        {
            // ✅ Payment success
            booking.Status = "Paid";

            if (payment != null)
            {
                payment.Status              = "Paid";
                payment.PaymentDate         = DateTime.Now;
                payment.VnpayTransactionId  = transactionId;
                payment.VnpayOrderInfo      = orderInfo;
            }

            // Confirm seats as booked (releases SeatHold records)
            var seatIds = booking.BookingDetails.Select(d => d.SeatId!.Value).ToList();
            await _seatService.ConfirmBookingAsync(seatIds, booking.ScheduleId!.Value);

            await _context.SaveChangesAsync();

            // Clear session
            HttpContext.Session.Remove("SelectedSeats");
            HttpContext.Session.Remove("PendingBookingId");
            HttpContext.Session.Remove("PendingPaymentId");
            HttpContext.Session.Remove("CheckoutScheduleId");

            return RedirectToPage("/Payment/Success", new { bookingId });
        }
        else
        {
            // ❌ Payment failed or cancelled
            booking.Status = "Cancelled";

            if (payment != null)
            {
                payment.Status             = "Failed";
                payment.VnpayTransactionId = transactionId;
            }

            // Release held seats back to available
            var seatIds = booking.BookingDetails.Select(d => d.SeatId!.Value).ToList();
            var userId  = HttpContext.Session.GetInt32("UserId") ?? booking.UserId ?? 0;
            foreach (var seatId in seatIds)
                await _seatService.ReleaseSeatAsync(seatId, userId, booking.ScheduleId!.Value);

            await _context.SaveChangesAsync();

            return RedirectToPage("/Payment/Fail", new { bookingId, code = responseCode });
        }
    }
}
