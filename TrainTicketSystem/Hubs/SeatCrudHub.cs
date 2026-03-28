using Microsoft.AspNetCore.SignalR;

namespace TrainTicketSystem.Hubs;

public class SeatCrudHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "seats");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "seats");
        await base.OnDisconnectedAsync(exception);
    }
}
