using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.Seats;

public class IndexModel : PageModel
{
    private readonly TrainTicketDbContext _context;

    public IndexModel(TrainTicketDbContext context)
    {
        _context = context;
    }

    // ── Hiển thị danh sách ──────────────────────────────────────────
    public IList<SeatViewModel> Seats { get; set; } = new List<SeatViewModel>();
    public SelectList TrainOptions { get; set; } = default!;
    public SelectList SeatTypeOptions { get; set; } = default!;

    // ── Filter (GET) ────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)]
    public int? FilterTrainId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FilterSeatTypeId { get; set; }

    // ── Form data cho Create / Edit modal ───────────────────────────
    // [BindProperty] cho phép Razor Pages tự map form fields vào object này
    // khi có POST request, không cần đọc từng field thủ công.
    [BindProperty]
    public SeatFormModel CurrentSeat { get; set; } = new();

    // ── GET ─────────────────────────────────────────────────────────
    public async Task OnGetAsync()
    {
        await PopulateDropdownsAsync();

        var query = _context.Seats
            .Include(s => s.Train)
            .Include(s => s.SeatType)
            .AsQueryable();

        if (FilterTrainId.HasValue)
            query = query.Where(s => s.TrainId == FilterTrainId.Value);

        if (FilterSeatTypeId.HasValue)
            query = query.Where(s => s.SeatTypeId == FilterSeatTypeId.Value);

        Seats = await query
            .Select(s => new SeatViewModel
            {
                SeatId = s.SeatId,
                TrainName = s.Train!.TrainName ?? "N/A",
                SeatNumber = s.SeatNumber ?? "",
                TypeName = s.SeatType!.TypeName ?? "N/A",
                PriceMultiplier = s.SeatType!.PriceMultiplier ?? 0
            })
            .OrderBy(s => s.SeatId)
            .ToListAsync();
    }

    // ── POST: Create ────────────────────────────────────────────────
    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid data. Please check the form.";
            return RedirectToPage();
        }

        var seat = new Seat
        {
            TrainId = CurrentSeat.TrainId,
            SeatTypeId = CurrentSeat.SeatTypeId,
            SeatNumber = CurrentSeat.SeatNumber,
        };

        _context.Seats.Add(seat);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Seat \"{seat.SeatNumber}\" created successfully.";
        return RedirectToPage();
    }

    // ── POST: Edit ──────────────────────────────────────────────────
    public async Task<IActionResult> OnPostEditAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid data. Please check the form.";
            return RedirectToPage();
        }

        var seat = await _context.Seats.FindAsync(CurrentSeat.SeatId);
        if (seat == null)
        {
            TempData["ErrorMessage"] = "Seat not found.";
            return RedirectToPage();
        }

        // Chỉ update các field cần thiết, không tạo entity mới
        // → tránh mất dữ liệu ở các column không có trong form
        seat.TrainId = CurrentSeat.TrainId;
        seat.SeatTypeId = CurrentSeat.SeatTypeId;
        seat.SeatNumber = CurrentSeat.SeatNumber;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Seat \"{seat.SeatNumber}\" updated successfully.";
        return RedirectToPage();
    }

    // ── POST: Delete ────────────────────────────────────────────────
    // Nhận id qua route parameter thay vì BindProperty
    // vì Delete modal chỉ cần 1 giá trị duy nhất
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var seat = await _context.Seats.FindAsync(id);
        if (seat == null)
        {
            TempData["ErrorMessage"] = "Seat not found.";
            return RedirectToPage();
        }

        _context.Seats.Remove(seat);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Seat deleted successfully.";
        return RedirectToPage();
    }

    // ── Helper ──────────────────────────────────────────────────────
    private async Task PopulateDropdownsAsync()
    {
        var trains = await _context.Trains
            .OrderBy(t => t.TrainName)
            .ToListAsync();

        var seatTypes = await _context.SeatTypes
            .OrderBy(st => st.TypeName)
            .ToListAsync();

        TrainOptions = new SelectList(trains, "TrainId", "TrainName");
        SeatTypeOptions = new SelectList(seatTypes, "SeatTypeId", "TypeName");
    }
}

// ── ViewModels ───────────────────────────────────────────────────────

/// <summary>
/// Dùng để hiển thị dữ liệu trong bảng (read-only).
/// Join sẵn TrainName và TypeName để View không cần xử lý.
/// </summary>
public class SeatViewModel
{
    public int SeatId { get; set; }
    public string TrainName { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public decimal PriceMultiplier { get; set; }
}

/// <summary>
/// Dùng để nhận dữ liệu từ form Create / Edit (write).
/// Tách khỏi SeatViewModel vì form cần ID (TrainId, SeatTypeId),
/// còn bảng cần Name (TrainName, TypeName).
/// </summary>
public class SeatFormModel
{
    public int SeatId { get; set; }
    public int TrainId { get; set; }
    public int SeatTypeId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public decimal PriceMultiplier { get; set; }
}