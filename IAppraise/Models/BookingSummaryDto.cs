namespace IAppraise.Models
{
    public class BookingSummaryDto
    {
        public int BookingId { get; set; }

        public string? CustomerFirstName { get; set; }
        public string? CustomerLastName { get; set; }
        public string? CustomerPhoneNumber { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerSuburb { get; set; }
        public string? CustomerTitle { get; set; }

        public string? DealerFirstName { get; set; }
        public string? DealerLastName { get; set; }

        public DateTime? DateTimeStarted { get; set; }
    }
}
