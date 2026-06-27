namespace IAppraise.Contracts
{
    public class PickupResponseDto
    {
        public int BookingId { get; set; }
        public int IAppraiseVehicleId { get; set; }
        public bool VehicleCreated { get; set; }
    }
}
