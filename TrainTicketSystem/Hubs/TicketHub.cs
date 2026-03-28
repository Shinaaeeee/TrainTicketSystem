using Microsoft.AspNetCore.SignalR;

namespace TrainTicketSystem.Hubs;

public class TicketHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "tickets");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "tickets");
        await base.OnDisconnectedAsync(exception);
    }
}
