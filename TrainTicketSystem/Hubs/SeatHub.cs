using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using TrainTicketSystem.Services;

namespace TrainTicketSystem.Hubs;

/// <summary>
/// SignalR hub managing real-time seat status updates.
/// Clients join a group per schedule: "schedule-{scheduleId}"
/// </summary>
public class SeatHub : Hub
{
    private readonly ISeatService _seatService;

    // In-memory map: ConnectionId → session info (used for disconnect cleanup)
    // Note: this is for routing only — seat state lives in DB
    private static readonly ConcurrentDictionary<string, SeatConnectionInfo> _connections = new();

    public SeatHub(ISeatService seatService)
    {
        _seatService = seatService;
    }

    /// <summary>Called by client when entering the seat selection page.</summary>
    public async Task JoinScheduleGroup(int scheduleId, int userId)
    {
        _connections[Context.ConnectionId] = new SeatConnectionInfo(userId, scheduleId, IsInCheckout: false);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"schedule-{scheduleId}");
    }

    /// <summary>Called by client when explicitly leaving the page (optional, handled by disconnect too).</summary>
    public async Task LeaveScheduleGroup(int scheduleId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"schedule-{scheduleId}");
    }

    /// <summary>
    /// Client calls this when navigating to the checkout page.
    /// Seats are preserved for up to 10 minutes even if browser is closed.
    /// </summary>
    public void MarkInCheckout()
    {
        if (_connections.TryGetValue(Context.ConnectionId, out var info))
            _connections[Context.ConnectionId] = info with { IsInCheckout = true };
    }

    /// <summary>
    /// On disconnect:
    /// - If user was on seat selection page → release held seats immediately
    /// - If user was on checkout page → keep hold (10 min timeout handles cleanup)
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connections.TryRemove(Context.ConnectionId, out var info) && !info.IsInCheckout)
        {
            // User closed tab before reaching checkout — release their seats right away
            await _seatService.ReleaseByUserAsync(info.UserId, info.ScheduleId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>Tracks metadata for a SignalR connection.</summary>
public record SeatConnectionInfo(int UserId, int ScheduleId, bool IsInCheckout);
