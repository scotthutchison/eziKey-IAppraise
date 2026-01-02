using System.Text.Json.Serialization;

namespace Integrations.Dtos
{
    public class VehicleDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("make")]
        public string? Make { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("new_used")]
        public string? NewUsed { get; set; }

        [JsonPropertyName("stock_number")]
        public string? StockNumber { get; set; }

        [JsonPropertyName("vin_number")]
        public string? VinNumber { get; set; }

        [JsonPropertyName("registration_number")]
        public string? RegistrationNumber { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }

        [JsonPropertyName("colour")]
        public string? Colour { get; set; }

        [JsonPropertyName("model_year")]
        public int? ModelYear { get; set; }

        [JsonPropertyName("odometer")]
        public int Odometer { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("is_manual_entry")]
        public bool IsManualEntry { get; set; }

    }
}
