using System.Text.Json.Serialization;

namespace Integrations.Dtos
{
    public class CustomerDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("suburb")]
        public string? Suburb { get; set; }

        // API sends the string "null", not actual null
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

}
