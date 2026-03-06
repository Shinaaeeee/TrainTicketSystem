using System;
using System.Collections.Generic;

namespace TrainTicketSystem.Models;

public partial class Route
{
    public int RouteId { get; set; }

    public string? StartStation { get; set; }

    public string? EndStation { get; set; }

    public int? DistanceKm { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
