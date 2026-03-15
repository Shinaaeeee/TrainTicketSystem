using System;
using System.Collections.Generic;

namespace TrainTicketSystem.Models;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public int? TrainId { get; set; }

    public int? RouteId { get; set; }

    public DateTime? DepartureTime { get; set; }

    public DateTime? ArrivalTime { get; set; }
    public decimal? Price { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Route? Route { get; set; }

    public virtual Train? Train { get; set; }
}
