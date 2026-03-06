using System;
using System.Collections.Generic;

namespace TrainTicketSystem.Models;

public partial class Seat
{
    public int SeatId { get; set; }

    public int? TrainId { get; set; }

    public string? SeatNumber { get; set; }

    public int? SeatTypeId { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual SeatType? SeatType { get; set; }

    public virtual Train? Train { get; set; }
}
