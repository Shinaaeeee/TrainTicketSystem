using System;
using System.Collections.Generic;

namespace TrainTicketSystem.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Role { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
