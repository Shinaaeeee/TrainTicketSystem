namespace TrainTicketSystem.Models
{
    // ViewModel for the booking list (Index page)
    // Joins data from Booking, Users, Schedule, Route tables
    public class BookingViewModel
    {
        public int BookingId { get; set; }
        public string FullName { get; set; }       // from Users
        public string StartStation { get; set; }   // from Route
        public string EndStation { get; set; }     // from Route
        public string TrainName { get; set; }      // from Train
        public DateTime DepartureTime { get; set; } // from Schedule
        public DateTime BookingDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
    }

    // ViewModel for the booking detail page
    public class BookingDetailViewModel
    {
        // --- Booking header info ---
        public int BookingId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string TrainName { get; set; }
        public string StartStation { get; set; }
        public string EndStation { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }

        // --- Seat rows inside this booking ---
        public List<SeatRowViewModel> Seats { get; set; } = new();

        // --- Payment info (nullable — may not be paid yet) ---
        public string? PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
    }

    // One row in the BookingDetail table, enriched with seat info
    public class SeatRowViewModel
    {
        public string SeatNumber { get; set; }
        public string SeatTypeName { get; set; }
        public decimal Price { get; set; }
    }

    // ============================
    // Revenue ViewModels
    // ============================

    public class RevenueViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int PaidBookings { get; set; }
        public int PendingBookings { get; set; }

        public List<RevenueByRouteViewModel> ByRoute { get; set; } = new();
        public List<RevenueByMonthViewModel> ByMonth { get; set; } = new();

        // Filter params (bound from query string)
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class RevenueByRouteViewModel
    {
        public string StartStation { get; set; }
        public string EndStation { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RevenueByMonthViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalRevenue { get; set; }
        // Formatted label: "Tháng 4/2026"
        public string Label => $"Tháng {Month}/{Year}";
    }
}
