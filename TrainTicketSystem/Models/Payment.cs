using System;
using System.Collections.Generic;

namespace TrainTicketSystem.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? BookingId { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime? PaymentDate { get; set; }

    // VNPay tracking fields
    public string? VnpayTransactionId { get; set; }

    public string? VnpayOrderInfo { get; set; }

    /// <summary>Payment status: Pending | Paid | Failed</summary>
    public string? Status { get; set; }

    public virtual Booking? Booking { get; set; }
}
