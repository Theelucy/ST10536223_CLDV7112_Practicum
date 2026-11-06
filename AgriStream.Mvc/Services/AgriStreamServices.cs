using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using System.Text.Json;

namespace AgriStreamIOT.Mvc.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _container;

        public BlobStorageService(BlobContainerClient container)
        {
            _container = container;
        }

        public async Task<string> UploadAsync(Stream stream, string fileName)
        {
            await _container.CreateIfNotExistsAsync();
            var blob = _container.GetBlobClient(fileName);
            await blob.UploadAsync(stream, overwrite: true);
            return blob.Uri.ToString();
        }
    }

    public class TableStorageService
    {
        private readonly TableClient _tableClient;

        public TableStorageService(TableClient tableClient)
        {
            _tableClient = tableClient;
        }

        public async Task UpsertTelemetryAsync(string deviceName, double temperature, double humidity, string status)
        {
            await _tableClient.CreateIfNotExistsAsync();
            var entity = new TableEntity(deviceName, Guid.NewGuid().ToString())
            {
                { "Temperature", temperature },
                { "Humidity", humidity },
                { "Status", status },
                { "Timestamp", DateTime.UtcNow }
            };

            await _tableClient.UpsertEntityAsync(entity);
        }
    }

    public class IrrigationQueueService
    {
        private readonly QueueClient _queueClient;

        public IrrigationQueueService(QueueClient queueClient)
        {
            _queueClient = queueClient;
        }

        public async Task SendCommandAsync(string fieldId, string deviceName, string action, int durationMinutes)
        {
            await _queueClient.CreateIfNotExistsAsync();
            var payload = JsonSerializer.Serialize(new
            {
                CommandId = Guid.NewGuid().ToString(),
                FieldId = fieldId,
                DeviceName = deviceName,
                Action = action,
                DurationMinutes = durationMinutes,
                IssuedAtUtc = DateTime.UtcNow
            });

            await _queueClient.SendMessageAsync(payload);
        }
    }

    public class TelemetryService
    {
        private readonly TableClient _tableClient;

        public TelemetryService(TableClient tableClient)
        {
            _tableClient = tableClient;
        }

        public async Task<List<Dictionary<string, object>>> GetLastTelemetryAsync()
        {
            await _tableClient.CreateIfNotExistsAsync();
            var results = new List<Dictionary<string, object>>();

            await foreach (var entity in _tableClient.QueryAsync<TableEntity>())
            {
                var timestamp = entity["Timestamp"] as DateTime? ?? DateTime.UtcNow;
                var temperature = entity["Temperature"] as double? ?? 0;
                var humidity = entity["Humidity"] as double? ?? 0;
                var status = entity["Status"] as string ?? "Unknown";

                results.Add(new Dictionary<string, object>
                {
                    ["DeviceName"] = entity.PartitionKey,
                    ["Timestamp"] = timestamp.ToString("O"),
                    ["Temperature"] = temperature,
                    ["Humidity"] = humidity,
                    ["Status"] = status
                });
            }

            return results;
        }
    }
}
