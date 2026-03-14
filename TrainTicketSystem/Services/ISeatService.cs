using TrainTicketSystem.Models;

namespace TrainTicketSystem.Services;

/// <summary>
/// Contract for seat hold / release / booking operations.
/// All write operations are atomic to handle concurrent users safely.
/// </summary>
public interface ISeatService
{
    /// <summary>Attempt to hold a seat for a user. Returns false if already held/booked.</summary>
    Task<bool> TryHoldSeatAsync(int seatId, int userId, int scheduleId);

    /// <summary>Release a single seat that is currently held by the given user.</summary>
    Task ReleaseSeatAsync(int seatId, int userId, int scheduleId);

    /// <summary>Release all seats held by a user on a schedule (used on disconnect).</summary>
    Task ReleaseByUserAsync(int userId, int scheduleId);

    /// <summary>Confirm seats as Booked after successful payment.</summary>
    Task ConfirmBookingAsync(IEnumerable<int> seatIds, int scheduleId);

    /// <summary>Background job: release all seats whose HoldExpiredAt has passed.</summary>
    Task ReleaseExpiredHoldsAsync();

    /// <summary>Load all seats for a schedule's train.</summary>
    Task<List<SeatDto>> GetSeatsForScheduleAsync(int scheduleId, int currentUserId);
}

/// <summary>DTO returned to frontend — avoids sending full EF entity.</summary>
public class SeatDto
{
    public int SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string? SeatTypeName { get; set; }
    public decimal? PriceMultiplier { get; set; }

    // Status for UI: "Available", "Held", "Mine", "Booked"
    public string Status { get; set; } = "Available";
}
