namespace IAppraise.Contracts
{
    public class ReturnRequestDto
    {
        public int ReturningOdometer { get; set; }
        public string ReturningFuelLevel { get; set; } = string.Empty;
    }
}
