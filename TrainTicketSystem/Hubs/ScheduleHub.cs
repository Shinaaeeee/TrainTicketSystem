using Microsoft.AspNetCore.SignalR;

namespace TrainTicketSystem.Hubs;

/// <summary>
/// SignalR hub for pushing real-time schedule updates (CRUD) to all connected clients.
/// </summary>
public class ScheduleHub : Hub
{
    // Để trống: Server sẽ dùng IHubContext để chủ động gọi hàm xuống Client 
    // mỗi khi có thao tác Create, Edit, hoặc Delete thành công.
}