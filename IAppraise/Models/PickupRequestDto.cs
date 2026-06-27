namespace IAppraise.Models
{
    public class PickupRequestDto
    {
        public int? IAppraiseVehicleId { get; set; }

        public string? RegistrationNumber { get; set; }
        public string? StockNumber { get; set; }

        public bool CreateIfMissing { get; set; } = true;

        public string? Make { get; set; }
        public string? Model { get; set; }
        public int? ModelYear { get; set; }
        public string? VinNumber { get; set; }
        public string? Colour { get; set; }
        public int? Odometer { get; set; }
        public string? NewUsed { get; set; }
    }
}
