using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgriStreamIOT.Data;
using AgriStreamIOT.Mvc.Services;

namespace AgriStreamIOT.Mvc.Controllers
{
    public class AgriStreamController : Controller
    {
        private readonly AgriStreamDbContext _context;
        private readonly AzureFunctionsApiClient _functionsClient;
        private readonly IrrigationQueueService _queueService;

        public AgriStreamController(AgriStreamDbContext context, AzureFunctionsApiClient functionsClient, IrrigationQueueService queueService)
        {
            _context = context;
            _functionsClient = functionsClient;
            _queueService = queueService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var fields = await _context.Fields
                .Include(f => f.Devices)
                .Include(f => f.Owner)
                .ToListAsync();

            // Get telemetry data from Azure Functions
            var telemetryData = await _functionsClient.GetTelemetryDataAsync();
            ViewData["TelemetryData"] = telemetryData;

            return View(fields);
        }

        [HttpPost]
        public async Task<IActionResult> SendIrrigationCommand(string deviceName, string action, int durationMinutes)
        {
            var success = await _functionsClient.SendIrrigationCommandAsync("field-001", deviceName, action, durationMinutes);
            
            if (success)
            {
                TempData["Message"] = $"? Irrigation command for {deviceName} was queued successfully!";
            }
            else
            {
                TempData["Error"] = $"? Failed to queue irrigation command for {deviceName}";
            }

            return RedirectToAction(nameof(Dashboard));
        }
    }
}
