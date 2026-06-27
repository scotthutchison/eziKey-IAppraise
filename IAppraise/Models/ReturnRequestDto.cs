namespace IAppraise.Models
{
    public class ReturnRequestDto
    {
        public int? ReturningOdometer { get; set; }

        public string ReturningFuelLevel { get; set; } = "Full";
    }
}
