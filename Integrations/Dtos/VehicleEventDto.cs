using System.Text.Json.Serialization;

namespace Integrations.Dtos
{

    public class VehicleEventsResponse
    {
        [JsonPropertyName("vehicle_events")]
        public List<VehicleEventDto> VehicleEvents { get; set; } = new();
    }

    public class VehicleEventDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("dealer")]
        public DealerDto Dealer { get; set; } = null!;

        [JsonPropertyName("customer")]
        public CustomerDto Customer { get; set; } = null!;

        [JsonPropertyName("date_time_started")]
        public DateTime? DateTimeStarted { get; set; }
    }
}
