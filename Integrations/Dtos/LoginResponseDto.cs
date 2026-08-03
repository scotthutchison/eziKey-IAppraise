using System.Text.Json.Serialization;

namespace Integrations.Dtos
{
    public class LoginResponseDto
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }
    }
}
