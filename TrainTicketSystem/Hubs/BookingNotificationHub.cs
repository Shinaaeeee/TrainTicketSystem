using Microsoft.AspNetCore.SignalR;

namespace TrainTicketSystem.Hubs;

/// <summary>
/// SignalR hub for pushing real-time booking notifications to admin dashboard.
/// Admin clients join the "admin" group on connect.
/// </summary>
public class BookingNotificationHub : Hub
{
    /// <summary>Called by admin clients to subscribe to booking notifications.</summary>
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admin");
        await base.OnDisconnectedAsync(exception);
    }
}
