using System;
using System.Collections.Generic;

namespace TrainTicketSystem.Models;

public partial class Train
{
    public int TrainId { get; set; }

    public string? TrainName { get; set; }

    public int? Capacity { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
