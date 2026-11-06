using System.Text;
using System.Text.Json;

namespace AgriStreamIOT.Mvc.Services
{
    public class AzureFunctionsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AzureFunctionsApiClient> _logger;
        private readonly IConfiguration _configuration;

        public AzureFunctionsApiClient(HttpClient httpClient, ILogger<AzureFunctionsApiClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<List<TelemetryData>> GetTelemetryDataAsync()
        {
            try
            {
                var functionUrl = _configuration["AzureFunctions:TelemetryUrl"] ?? "http://localhost:7071/api/telemetry";
                var response = await _httpClient.GetAsync(functionUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var telemetry = JsonSerializer.Deserialize<List<TelemetryData>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return telemetry ?? new List<TelemetryData>();
                }

                _logger.LogWarning("Failed to fetch telemetry data. Status: {StatusCode}", response.StatusCode);
                return new List<TelemetryData>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching telemetry data");
                return new List<TelemetryData>();
            }
        }

        public async Task<bool> SendIrrigationCommandAsync(string fieldId, string deviceName, string action, int durationMinutes)
        {
            try
            {
                var functionUrl = _configuration["AzureFunctions:IrrigationCommandUrl"] ?? "http://localhost:7071/api/irrigation-command";
                var command = new
                {
                    commandId = Guid.NewGuid().ToString(),
                    fieldId,
                    deviceName,
                    action,
                    durationMinutes,
                    issuedAtUtc = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(command);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(functionUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Irrigation command sent successfully for {DeviceName}", deviceName);
                    return true;
                }

                _logger.LogWarning("Failed to send irrigation command. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending irrigation command");
                return false;
            }
        }
    }

    public class TelemetryData
    {
        public string DeviceName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public DateTime LastUpdated { get; set; }
        public string FieldName { get; set; } = string.Empty;
    }
}
