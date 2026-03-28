using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Hubs;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Services;

public class SeatService : ISeatService
{
    private readonly TrainTicketDbContext _context;
    private readonly IHubContext<SeatHub> _hubContext;

    public SeatService(TrainTicketDbContext context, IHubContext<SeatHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<bool> TryHoldSeatAsync(int seatId, int userId, int scheduleId)
    {
        var rows = await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE Seat
              SET SeatHoldStatus = 'Held',
                  HoldExpiredAt  = DATEADD(MINUTE, 10, GETUTCDATE()),
                  HeldByUserId   = @userId
              WHERE SeatId = @seatId
                AND (SeatHoldStatus IS NULL OR SeatHoldStatus = 'Available')",
            new SqlParameter("@userId", userId),
            new SqlParameter("@seatId", seatId));

        if (rows > 0)
        {
            
            await _hubContext.Clients
                .Group($"schedule-{scheduleId}")
                .SendAsync("SeatStatusChanged", new { seatId, status = "Held", heldByUserId = userId });
        }

        return rows > 0;
    }

    public async Task ReleaseSeatAsync(int seatId, int userId, int scheduleId)
    {
        var rows = await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE Seat
              SET SeatHoldStatus = NULL,
                  HoldExpiredAt  = NULL,
                  HeldByUserId   = NULL
              WHERE SeatId = @seatId AND HeldByUserId = @userId AND SeatHoldStatus = 'Held'",
            new SqlParameter("@seatId", seatId),
            new SqlParameter("@userId", userId));

        if (rows > 0)
        {
            await _hubContext.Clients
                .Group($"schedule-{scheduleId}")
                .SendAsync("SeatStatusChanged", new { seatId, status = "Available", heldByUserId = (int?)null });
        }
    }

    public async Task ReleaseByUserAsync(int userId, int scheduleId)
    {
        var heldSeats = await _context.Seats
            .Where(s => s.HeldByUserId == userId && s.SeatHoldStatus == "Held"
                     && s.Train!.Schedules.Any(sc => sc.ScheduleId == scheduleId))
            .Select(s => s.SeatId)
            .ToListAsync();

        if (!heldSeats.Any()) return;

        await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE Seat
              SET SeatHoldStatus = NULL, HoldExpiredAt = NULL, HeldByUserId = NULL
              WHERE HeldByUserId = @userId AND SeatHoldStatus = 'Held'",
            new SqlParameter("@userId", userId));

        foreach (var seatId in heldSeats)
        {
            await _hubContext.Clients
                .Group($"schedule-{scheduleId}")
                .SendAsync("SeatStatusChanged", new { seatId, status = "Available", heldByUserId = (int?)null });
        }
    }

    public async Task ConfirmBookingAsync(IEnumerable<int> seatIds, int scheduleId)
    {
        var ids = string.Join(",", seatIds);

        await _context.Database.ExecuteSqlRawAsync(
            $@"UPDATE Seat
               SET SeatHoldStatus = 'Booked', HoldExpiredAt = NULL
               WHERE SeatId IN ({ids}) AND SeatHoldStatus = 'Held'");

        foreach (var seatId in seatIds)
        {
            await _hubContext.Clients
                .Group($"schedule-{scheduleId}")
                .SendAsync("SeatStatusChanged", new { seatId, status = "Booked", heldByUserId = (int?)null });
        }
    }

    public async Task ReleaseExpiredHoldsAsync()
    {
        var expired = await _context.Seats
            .Where(s => s.SeatHoldStatus == "Held" && s.HoldExpiredAt < DateTime.UtcNow)
            .Include(s => s.Train)
                .ThenInclude(t => t!.Schedules)
            .ToListAsync();

        if (!expired.Any()) return;

        await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE Seat
              SET SeatHoldStatus = NULL, HoldExpiredAt = NULL, HeldByUserId = NULL
              WHERE SeatHoldStatus = 'Held' AND HoldExpiredAt < GETUTCDATE()");

        foreach (var seat in expired)
        {
            foreach (var schedule in seat.Train?.Schedules ?? [])
            {
                await _hubContext.Clients
                    .Group($"schedule-{schedule.ScheduleId}")
                    .SendAsync("SeatStatusChanged", new { seatId = seat.SeatId, status = "Available", heldByUserId = (int?)null });
            }
        }
    }


    public async Task<List<SeatDto>> GetSeatsForScheduleAsync(int scheduleId, int currentUserId)
    {
        var schedule = await _context.Schedules
            .Include(s => s.Train)
                .ThenInclude(t => t!.Seats)
                    .ThenInclude(s => s.SeatType)
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

        if (schedule?.Train == null) return [];

        return schedule.Train.Seats
            .OrderBy(s => s.SeatNumber)
            .Select(s => new SeatDto
            {
                SeatId          = s.SeatId,
                SeatNumber      = s.SeatNumber ?? "",
                SeatTypeName    = s.SeatType?.TypeName,
                PriceMultiplier = s.SeatType?.PriceMultiplier,
                Status = s.SeatHoldStatus == "Booked" ? "Booked"
                       : s.SeatHoldStatus == "Held" && s.HeldByUserId == currentUserId ? "Mine"
                       : s.SeatHoldStatus == "Held" ? "Held"
                       : "Available"
            }).ToList();
    }
}
