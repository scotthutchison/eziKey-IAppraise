using System.Text.Json.Serialization;

namespace Integrations.Dtos
{
    /// <summary>
    /// Body for POST /api/vehicle/create-ezikey-vehicle/ — only <see cref="Dealership"/> is required.
    /// </summary>
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

        // TDL expects "New", "Used" or "Demo".
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
