using Microsoft.AspNetCore.SignalR;

namespace TrainTicketSystem.Hubs;

public class ScheduleHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "schedules");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "schedules");
        await base.OnDisconnectedAsync(exception);
    }
}
