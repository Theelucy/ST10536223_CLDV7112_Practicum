using System.Text.Json.Serialization;

namespace AgriStreamIOT.Functions.Models
{
    public class IrrigationCommand
    {
        [JsonPropertyName("commandId")]
        public string CommandId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("fieldId")]
        public string FieldId { get; set; } = string.Empty;

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = "Start";

        [JsonPropertyName("durationMinutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("issuedAtUtc")]
        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
