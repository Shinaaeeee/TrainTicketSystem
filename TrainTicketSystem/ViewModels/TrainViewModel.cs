namespace TrainTicketSystem.ViewModels
{
    public class TrainViewModel
    {
        public int ScheduleId { get; set; }

        public string TrainName { get; set; }

        public string FromStation { get; set; }

        public string ToStation { get; set; }

        public DateTime? DepartureTime { get; set; }

        public string SeatType { get; set; }

        public decimal? Price { get; set; }
    }

}
