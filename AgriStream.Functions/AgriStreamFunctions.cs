using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AgriStreamIOT.Data;
using AgriStreamIOT.Functions.Models;

namespace AgriStreamIOT.Functions
{
    public class AgriStreamFunctions
    {
        private readonly AgriStreamDbContext _dbContext;
        private readonly ILogger<AgriStreamFunctions> _logger;
        private readonly BlobContainerClient _blobContainerClient;

        public AgriStreamFunctions(
            AgriStreamDbContext dbContext,
            ILogger<AgriStreamFunctions> logger,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;

            var accountName = configuration["AzureStorage:AccountName"] ?? "agristreamiot";
            var accountKey = configuration["AzureStorage:AccountKey"] ?? string.Empty;

            _blobContainerClient = new BlobContainerClient(
                new Uri($"https://{accountName}.blob.core.windows.net/drone-images"),
                new Azure.Storage.StorageSharedKeyCredential(accountName, accountKey));
        }

        [Function("ProcessDroneImageBlob")]
        public async Task ProcessDroneImageBlob(
            [BlobTrigger("drone-images/{name}", Connection = "AzureWebJobsStorage")] BlobClient blob,
            string name)
        {
            _logger.LogInformation("Blob trigger processed: {BlobName}", name);
            _logger.LogInformation("Blob size: {BlobSize} bytes", blob.GetProperties().Value.ContentLength);

            var metadata = await blob.GetPropertiesAsync();
            if (metadata.Value.Metadata.TryGetValue("fieldId", out var fieldId))
            {
                _logger.LogInformation("Image linked to field {FieldId}", fieldId);
            }
        }

        [Function("GetTelemetryData")]
        public async Task<HttpResponseData> GetTelemetryData(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "telemetry")] HttpRequestData req)
        {
            var telemetry = await _dbContext.DeviceStatuses
                .Include(d => d.Field)
                .OrderByDescending(d => d.LastUpdated)
                .Take(20)
                .Select(d => new
                {
                    d.DeviceName,
                    d.Status,
                    d.Temperature,
                    d.Humidity,
                    d.LastUpdated,
                    FieldName = d.Field.Name
                })
                .ToListAsync();

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(telemetry);
            return response;
        }

        [Function("SendIrrigationCommand")]
        public async Task<HttpResponseData> SendIrrigationCommand(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "irrigation-command")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var command = JsonSerializer.Deserialize<IrrigationCommand>(body);

            if (command == null)
            {
                var badRequest = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid irrigation command payload.");
                return badRequest;
            }

            var storageConnection = Environment.GetEnvironmentVariable("StorageQueueConnection")
                ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true";

            var queueClient = new Azure.Storage.Queues.QueueClient(storageConnection, "irrigation-queue");

            await queueClient.CreateIfNotExistsAsync();
            await queueClient.SendMessageAsync(JsonSerializer.Serialize(command));

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message = "Irrigation command queued successfully", commandId = command.CommandId });
            return response;
        }

        [Function("ProcessIrrigationQueue")]
        public async Task ProcessIrrigationQueue(
            [QueueTrigger("irrigation-queue", Connection = "StorageQueueConnection")] QueueMessage message)
        {
            var payload = message.MessageText;
            _logger.LogInformation("Processing irrigation command: {Message}", payload);

            var command = JsonSerializer.Deserialize<IrrigationCommand>(payload);
            if (command == null)
            {
                _logger.LogWarning("Received invalid irrigation command payload.");
                return;
            }

            var device = await _dbContext.DeviceStatuses
                .FirstOrDefaultAsync(d => d.DeviceName == command.DeviceName);

            if (device == null)
            {
                _logger.LogWarning("No device found for irrigation command: {DeviceName}", command.DeviceName);
                return;
            }

            device.Status = command.Action == "Stop" ? "Idle" : "Irrigation Active";
            device.LastUpdated = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Irrigation command executed for device {DeviceName}", command.DeviceName);
        }
    }
}
