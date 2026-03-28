using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Models;
using TrainTicketSystem.Services;

namespace TrainTicketSystem.Pages.Booking;

public class SeatMapModel : PageModel
{
    private readonly TrainTicketDbContext _context;
    private readonly ISeatService _seatService;

    public SeatMapModel(TrainTicketDbContext context, ISeatService seatService)
    {
        _context = context;
        _seatService = seatService;
    }

    public Schedule? Schedule { get; set; }
    public List<SeatDto> Seats { get; set; } = [];
    public int CurrentUserId { get; set; }

    [BindProperty] public int SeatId { get; set; }
    [BindProperty] public int ScheduleId { get; set; }
    [BindProperty] public List<int> SelectedSeatIds { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int scheduleId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Login");

        CurrentUserId = userId.Value;
        ScheduleId = scheduleId;

        Schedule = await _context.Schedules
            .Include(s => s.Route)
            .Include(s => s.Train)
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

        if (Schedule == null) return NotFound();

        Seats = await _seatService.GetSeatsForScheduleAsync(scheduleId, CurrentUserId);

        return Page();
    }

    public async Task<IActionResult> OnPostHoldAsync(int seatId, int scheduleId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return new JsonResult(new { success = false, message = "Vui lòng đăng nhập lại." });

        var success = await _seatService.TryHoldSeatAsync(seatId, userId.Value, scheduleId);

        return new JsonResult(new
        {
            success,
            message = success ? "Ghế đã được giữ cho bạn trong 10 phút." : "Ghế đã bị người khác giữ trước. Vui lòng chọn ghế khác."
        });
    }

    public async Task<IActionResult> OnPostReleaseAsync(int seatId, int scheduleId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return new JsonResult(new { success = false });

        await _seatService.ReleaseSeatAsync(seatId, userId.Value, scheduleId);
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostCheckoutAsync(List<int> selectedSeatIds, int scheduleId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Login");

        if (!selectedSeatIds.Any())
        {
            TempData["Error"] = "Vui lòng chọn ít nhất 1 ghế.";
            return RedirectToPage(new { scheduleId });
        }

        if (selectedSeatIds.Count > 5)
        {
            TempData["Error"] = "Tối đa 5 ghế mỗi lần đặt.";
            return RedirectToPage(new { scheduleId });
        }

        HttpContext.Session.SetString("SelectedSeats", string.Join(",", selectedSeatIds));
        HttpContext.Session.SetInt32("CheckoutScheduleId", scheduleId);

        return RedirectToPage("/Booking/Checkout", new { scheduleId });
    }
}
