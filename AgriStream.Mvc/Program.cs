using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;
using AgriStreamIOT.Data;
using AgriStreamIOT.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Register Entity Framework Core for User Management
builder.Services.AddDbContext<AgriStreamDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CommerceConnection")));

// Register Azure Storage Services
var storageAccountName = builder.Configuration["AzureStorage:AccountName"] ?? string.Empty;
var storageAccountKey = builder.Configuration["AzureStorage:AccountKey"] ?? string.Empty;
var storageConnectionString = builder.Configuration.GetConnectionString("StorageQueue") ?? string.Empty;

// Blob Storage Client
builder.Services.AddSingleton(x => new BlobContainerClient(
    new Uri($"https://{storageAccountName}.blob.core.windows.net/drone-images"),
    new Azure.Storage.StorageSharedKeyCredential(storageAccountName, storageAccountKey)));

// Table Storage Client for Telemetry Data
builder.Services.AddSingleton(x => new TableClient(
    new Uri($"https://{storageAccountName}.table.core.windows.net/"),
    "SensorTelemetry",
    new TableSharedKeyCredential(storageAccountName, storageAccountKey),
    new TableClientOptions()));

// Queue Storage Client for Irrigation Commands
builder.Services.AddSingleton(x => new QueueClient(
    storageConnectionString,
    "irrigation-queue",
    new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));

// Register Application Services
builder.Services.AddScoped<BlobStorageService>();
builder.Services.AddScoped<TableStorageService>();
builder.Services.AddScoped<IrrigationQueueService>();
builder.Services.AddScoped<TelemetryService>();
builder.Services.AddHttpClient<AzureFunctionsApiClient>();
builder.Services.AddScoped<DatabaseSeedingService>();

var app = builder.Build();

// Use a fresh database name to avoid conflicting with older schema/state.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgriStreamDbContext>();

    try
    {
        db.Database.Migrate();
    }
    catch
    {
        db.Database.EnsureCreated();
    }

    var seedingService = scope.ServiceProvider.GetRequiredService<DatabaseSeedingService>();
    await seedingService.SeedAsync();
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();