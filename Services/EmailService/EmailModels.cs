namespace Services.Mail
{
    public class RegistrationNotificationModel
    {
        public string Name { get; set; }
    }

    public class UnsubcribeFromTourNotificationModel
    {
        public string Name { get; set; }
        public string TourName { get; set; }
    }

    public class TourRegistrationNotificationModel
    {
        public string Name { get; set; }
        public string TourName { get; set; }
        public string Price { get; set; }
        public DateTime StartDate { get; set; }
        public string AgencyName { get; set; }
        public string AgencyContact { get; set; }
        public List<string> Managers { get; set; }
    }

    public class UpcomingTourNotificationModel
    {
        public string Name { get; set; }
        public string TourName { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class BookingRequestNotificationModel
    {
        public string Name { get; set; }
        public string TourName { get; set; }
        public DateTime StartDate { get; set; }
        public string? TransferBookingLink { get; set; } = null;
        public List<(string SideName, string Link)> SidesLinks { get; set; } = new();
    }

    public class FeedbackRequestNotificationModel
    {
        public string Name { get; set; }
        public string TourName { get; set; }
        public string AgencyInfo { get; set; }
    }
}
