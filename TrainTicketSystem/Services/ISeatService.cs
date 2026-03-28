using TrainTicketSystem.Models;

namespace TrainTicketSystem.Services;

public interface ISeatService
{
    Task<bool> TryHoldSeatAsync(int seatId, int userId, int scheduleId);
    Task ReleaseSeatAsync(int seatId, int userId, int scheduleId);
    Task ReleaseByUserAsync(int userId, int scheduleId);
    Task ConfirmBookingAsync(IEnumerable<int> seatIds, int scheduleId);
    Task ReleaseExpiredHoldsAsync();
    Task<List<SeatDto>> GetSeatsForScheduleAsync(int scheduleId, int currentUserId);
}
public class SeatDto
{
    public int SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string? SeatTypeName { get; set; }
    public decimal? PriceMultiplier { get; set; }
    public string Status { get; set; } = "Available";
}
