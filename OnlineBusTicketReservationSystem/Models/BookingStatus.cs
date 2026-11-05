namespace OnlineBusTicketReservationSystem.Models
{
    public static class BookingStatus
    {
        public const string Pending   = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Cancelled = "Cancelled";
    }

    public static class PaymentStatus
    {
        public const string Pending = "Pending";
        public const string Paid    = "Paid";
    }

    public static class RefundStatus
    {
        public const string Pending   = "Pending";
        public const string Processed = "Processed";
    }

    public static class CancelledBy
    {
        public const string Customer = "Customer";
        public const string Admin    = "Admin";
    }
}
