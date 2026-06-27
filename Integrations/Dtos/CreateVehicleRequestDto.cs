using System.Text.Json.Serialization;

namespace Integrations.Dtos
{
    public class CreateVehicleRequestDto
    {
        [JsonPropertyName("dealership")]
        public int Dealership { get; set; }

        [JsonPropertyName("make")]
        public string? Make { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("model_year")]
        public int? ModelYear { get; set; }

        [JsonPropertyName("new_used")]
        public string? NewUsed { get; set; }

        [JsonPropertyName("stock_number")]
        public string? StockNumber { get; set; }

        [JsonPropertyName("registration_number")]
        public string? RegistrationNumber { get; set; }

        [JsonPropertyName("vin_number")]
        public string? VinNumber { get; set; }

        [JsonPropertyName("colour")]
        public string? Colour { get; set; }

        [JsonPropertyName("odometer")]
        public int? Odometer { get; set; }

        [JsonPropertyName("external_picture")]
        public string? ExternalPicture { get; set; }
    }
}
