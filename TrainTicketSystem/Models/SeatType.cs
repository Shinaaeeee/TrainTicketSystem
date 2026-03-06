using System;
using System.Collections.Generic;

namespace TrainTicketSystem.Models;

public partial class SeatType
{
    public int SeatTypeId { get; set; }

    public string? TypeName { get; set; }

    public decimal? PriceMultiplier { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
