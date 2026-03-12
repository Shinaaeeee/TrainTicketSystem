using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
//using YourApp.Data; // ← Đổi thành namespace thực tế của bạn
using TrainTicketSystem.Models;

namespace YourApp.Pages.Seats;

public class IndexModel : PageModel
{
    private readonly TrainTicketDbContext _context;

    public IndexModel(TrainTicketDbContext context)
    {
        _context = context;
    }

    // ---------------------------------------------------------------
    // Properties hiển thị dữ liệu ra View
    // ---------------------------------------------------------------

    /// <summary>
    /// Danh sách ghế đã được join với Train và SeatType, dùng để render bảng.
    /// Dùng ViewModel thay vì entity gốc để tránh expose dữ liệu thừa ra View.
    /// </summary>
    public IList<SeatViewModel> Seats { get; set; } = new List<SeatViewModel>();

    /// <summary>
    /// Danh sách tàu cho dropdown filter — SelectList tự động render <option>.
    /// </summary>
    public SelectList TrainOptions { get; set; } = default!;

    /// <summary>
    /// Danh sách loại ghế cho dropdown filter.
    /// </summary>
    public SelectList SeatTypeOptions { get; set; } = default!;

    // ---------------------------------------------------------------
    // Filter values — [SupportsGet] cho phép nhận giá trị qua GET query string
    // Ví dụ: /Seats?FilterTrainId=2&FilterSeatTypeId=1
    // Tại sao dùng GET thay vì POST? Vì filter không thay đổi dữ liệu,
    // và URL có thể bookmark/share được.
    // ---------------------------------------------------------------

    [BindProperty(SupportsGet = true)]
    public int? FilterTrainId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FilterSeatTypeId { get; set; }

    // ---------------------------------------------------------------
    // OnGetAsync — xử lý cả lần load đầu lẫn lúc filter
    // ---------------------------------------------------------------

    public async Task OnGetAsync()
    {
        await PopulateDropdownsAsync();

        // Dùng navigation properties thay vì manual Join.
        // EF Core tự dịch Include thành LEFT JOIN trong SQL.
        // Where filter trực tiếp trên FK — không cần subquery lồng.
        var query = _context.Seats
            .Include(s => s.Train)
            .Include(s => s.SeatType)
            .AsQueryable();

        // Filter trực tiếp trên FK của Seat — đơn giản, hiệu quả
        if (FilterTrainId.HasValue)
            query = query.Where(s => s.TrainId == FilterTrainId.Value);

        if (FilterSeatTypeId.HasValue)
            query = query.Where(s => s.SeatTypeId == FilterSeatTypeId.Value);

        // Project sang ViewModel SAU KHI đã filter — tránh load dữ liệu thừa
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

    // ---------------------------------------------------------------
    // Helper method — tách riêng để OnGetAsync gọn hơn
    // ---------------------------------------------------------------

    private async Task PopulateDropdownsAsync()
    {
        var trains = await _context.Trains
            .OrderBy(t => t.TrainName)
            .ToListAsync();

        var seatTypes = await _context.SeatTypes
            .OrderBy(st => st.TypeName)
            .ToListAsync();

        // dataValueField: giá trị submit lên server (ID)
        // dataTextField:  text hiển thị trong dropdown
        TrainOptions = new SelectList(trains, "TrainId", "TrainName");
        SeatTypeOptions = new SelectList(seatTypes, "SeatTypeId", "TypeName");
    }
}

// ---------------------------------------------------------------
// ViewModel — tại sao không dùng thẳng entity Seat?
// 1. Entity Seat không có TrainName hay TypeName — phải join.
// 2. ViewModel chỉ chứa đúng dữ liệu View cần, không expose FK thừa.
// 3. Tách biệt concern: entity là data layer, ViewModel là presentation layer.
// ---------------------------------------------------------------

public class SeatViewModel
{
    public int SeatId { get; set; }
    public string TrainName { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public decimal PriceMultiplier { get; set; }
}