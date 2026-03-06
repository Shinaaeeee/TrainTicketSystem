using System;
using System.Collections.Generic;

namespace TrainTicketSystem.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int? UserId { get; set; }

    public int? ScheduleId { get; set; }

    public DateTime? BookingDate { get; set; }

    public decimal? TotalPrice { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Schedule? Schedule { get; set; }

    public virtual User? User { get; set; }
}
