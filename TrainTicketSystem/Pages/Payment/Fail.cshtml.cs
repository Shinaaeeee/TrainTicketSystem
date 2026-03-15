using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TrainTicketSystem.Pages.Payment;

public class FailModel : PageModel
{
    public string? Reason { get; set; }
    public string? Code { get; set; }
    public int? BookingId { get; set; }

    public void OnGet(string? reason, string? code, int? bookingId)
    {
        Reason    = reason;
        Code      = code;
        BookingId = bookingId;
    }
}
