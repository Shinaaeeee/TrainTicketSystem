using System;
using System.Collections.Generic;

namespace TrainTicketSystem.Models;

public partial class Seat
{
    public int SeatId { get; set; }

    public int? TrainId { get; set; }

    public string? SeatNumber { get; set; }

    public int? SeatTypeId { get; set; }

    // Hold tracking fields (added for real-time seat reservation feature)
    public string? SeatHoldStatus { get; set; }   // null/'Available', 'Held', 'Booked'
    public DateTime? HoldExpiredAt { get; set; }  // Expiry of the hold window
    public int? HeldByUserId { get; set; }        // Which user is currently holding this seat

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual SeatType? SeatType { get; set; }

    public virtual Train? Train { get; set; }
}
