# AgriStreamIOT - Real-Time Agricultural IoT Platform

## ? IMPLEMENTATION SUMMARY

### Project Structure
- **AgriStream.Mvc** - ASP.NET Core MVC web application for dashboard and management
- **AgriStream.Data** - Entity Framework Core data layer with SQL Database
- **AgriStream.Functions** - Azure Functions for serverless data processing

---

## ??? ARCHITECTURE COMPONENTS

### 1. **Web Hosting - Azure App Service**
- Hosts AgriStream.Mvc application
- ASP.NET Core 8.0
- Connected to Azure SQL Database

### 2. **Relational Database - Azure SQL Database**
- Server: `agristream-sql-st10536223.database.windows.net`
- Database: `agristream-db`
- **Tables:**
  - `Users` - Login credentials and user management
  - `Roles` - System roles (Farmer, Agronomist, Admin)
  - `UserRoles` - User-to-role mapping
  - `Fields` - Farm field information
  - `DeviceStatuses` - IoT device status and telemetry

### 3. **Cloud Storage - Azure Storage**

#### Azure Blob Storage
- **Container:** `drone-images`
- **Purpose:** Store high-resolution aerial images from field drones
- **Usage:** Media management for field monitoring

#### Azure Table Storage
- **Table:** `SensorTelemetry`
- **Purpose:** Store high-frequency, non-relational telemetry data
- **Data:** Real-time sensor readings, irrigation events, environmental logs

#### Azure Queue Storage
- **Queue:** `irrigation-queue`
- **Purpose:** Queue irrigation commands for processing
- **Usage:** Asynchronous command processing

---

## ?? AZURE FUNCTIONS (4 Required)

### 1?? **ProcessDroneImageBlob** - Blob Trigger
```
Trigger: Azure Blob Storage (drone-images container)
Functionality:
  - Triggered when drone image is uploaded
  - Logs upload details
  - Writes processing status audit entry to Azure Table Storage
  - Extracts metadata (fieldId) from blob properties
```

### 2?? **GetTelemetryData** - HTTP GET Trigger
```
Trigger: HTTP GET Request
Route: /api/telemetry
Functionality:
  - Called from MVC dashboard when page loads
  - Retrieves latest telemetry data from Azure SQL Database
  - Returns sensor readings, temperatures, humidity
  - Enables near-instant rendering on dashboard
```

### 3?? **SendIrrigationCommand** - HTTP POST Trigger
```
Trigger: HTTP POST Request
Route: /api/irrigation-command
Functionality:
  - Called when user clicks "Trigger Irrigation" button
  - Receives irrigation command payload (deviceName, action, duration)
  - Places command message into Azure Storage Queue
  - Returns confirmation with commandId
```

### 4?? **ProcessIrrigationQueue** - Queue Trigger
```
Trigger: Azure Queue Storage (irrigation-queue)
Functionality:
  - Listens to irrigation command queue
  - Processes queued commands asynchronously
  - Updates device operational status in SQL Database
  - Records command execution timestamp
```

---

## ?? MVC APPLICATION FEATURES

### Home Page (`/Home/Index`)
- ? Welcome screen with AgriStreamIOT branding
- ? Feature overview (Real-Time Monitoring, Smart Irrigation, Drone Analytics)
- ? Link to Dashboard

### Dashboard (`/AgriStream/Dashboard`)
- ? Real-time telemetry data display
  - Device name, field, status, temperature, humidity
  - Last updated timestamp
  - Fetches from GetTelemetryData HTTP function
  
- ? Field management interface
  - Lists all agricultural fields
  - Shows connected IoT devices
  - Field location and owner information
  
- ? Irrigation control
  - Select action (Start/Stop)
  - Set duration in minutes
  - Queue commands via SendIrrigationCommand HTTP function
  - Real-time feedback with success/error messages

### Privacy Page (`/Home/Privacy`)
- ? Standard privacy policy page

---

## ?? DATA FLOW

```
???????????????
? Web Browser ?
???????????????
       ?
       ???? GET /AgriStream/Dashboard
       ?         ???? Fetch Fields from SQL DB
       ?         ???? Call GetTelemetryData Azure Function
       ?                 ???? Return telemetry data (JSON)
       ?
       ???? POST /AgriStream/SendIrrigationCommand
            ???? Call SendIrrigationCommand Azure Function
                 ???? Queue message to irrigation-queue
                      ???? ProcessIrrigationQueue Function
                           ???? Update DeviceStatus in SQL DB

???????????????????????
?  Drone Image Upload ?
???????????????????????
           ?
           ???? Azure Blob Storage
                ???? ProcessDroneImageBlob (Blob Trigger)
                     ???? Write audit log to Table Storage
```

---

## ?? AGRICULTURAL THEME

### Color Scheme
- **Primary Green:** #2d8659 (Agricultural IoT theme)
- **Dark Green:** #1a4d2e (Gradients and hover states)
- **White Text:** High contrast navigation

### UI Elements
- Dashboard with emoji indicators (????????)
- Status badges (? Healthy, ?? Irrigation Active, ? Alert)
- Card-based feature layout
- Responsive Bootstrap grid

---

## ?? CONFIGURATION

### appsettings.json (MVC)
```json
{
  "ConnectionStrings": {
    "CommerceConnection": "Server=tcp:<your-sql-server>.database.windows.net,1433;Initial Catalog=<your-database>;...",
    "StorageQueue": "DefaultEndpointsProtocol=https;AccountName=<your-storage-account>;..."
  },
  "AzureStorage": {
    "AccountName": "<your-storage-account>",
    "AccountKey": "<your-storage-account-key>"
  },
  "AzureFunctions": {
    "TelemetryUrl": "http://localhost:7071/api/telemetry",
    "IrrigationCommandUrl": "http://localhost:7071/api/irrigation-command"
  }
}
```

### local.settings.json (Functions)
```json
{
  "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=<your-storage-account>;...",
  "StorageQueueConnection": "DefaultEndpointsProtocol=https;AccountName=<your-storage-account>;...",
  "SqlConnection": "Server=tcp:<your-sql-server>.database.windows.net,1433;..."
}
```

---

## ?? SERVICES

### AzureFunctionsApiClient
- Calls Azure Functions HTTP endpoints
- Handles TelemetryData retrieval
- Sends irrigation commands

### BlobStorageService
- Manages drone image uploads
- Handles file storage operations

### TableStorageService
- Manages telemetry data in Table Storage
- CRUD operations for sensor readings

### IrrigationQueueService
- Sends commands to queue
- JSON serialization of command payloads

### TelemetryService
- Retrieves telemetry from Table Storage
- Aggregates sensor data

---

## ?? RUNNING THE APPLICATION

### Local Development
1. Update connection strings in appsettings.json
2. Run Azure Storage Emulator or use cloud storage
3. Build solution: `dotnet build`
4. Run MVC: `dotnet run --project AgriStream.Mvc`
5. Run Functions: `func start` (in AgriStream.Functions directory)

### Production Deployment
1. Deploy AgriStream.Mvc to Azure App Service
2. Deploy AgriStream.Functions to Azure Functions
3. Configure connection strings in Azure Portal
4. Update function URLs in App Service configuration

---

## ? REQUIREMENTS MET

? **Web Hosting:** Azure App Service (MVC)
? **Relational Database:** Azure SQL Database (User/Field/Device management)
? **Blob Storage:** Drone image uploads
? **Table Storage:** Telemetry data
? **Queue Storage:** Irrigation commands
? **4 Azure Functions:**
  - Blob Trigger (ProcessDroneImageBlob)
  - HTTP GET (GetTelemetryData)
  - HTTP POST (SendIrrigationCommand)
  - Queue Trigger (ProcessIrrigationQueue)
? **MVC Dashboard:** Real-time field and device management
? **Responsive UI:** Agricultural theme with green color scheme
? **Stateless Architecture:** Functions and App Service are horizontally scalable

---

## ?? NEXT STEPS FOR YOUR SUBMISSION

1. **Deploy to Azure**
   - Publish AgriStream.Mvc to App Service
   - Publish AgriStream.Functions to Azure Functions
   - Update configuration URLs

2. **Screenshot Evidence**
   - Azure SQL Database tables
   - Blob Storage containers
   - Table Storage tables
   - Queue Storage
   - App Service running
   - Functions deployed

3. **Load Testing**
   - Use Azure Load Testing or Apache JMeter
   - Document auto-scaling behavior

4. **Theory Answers**
   - Disaster Recovery & Redundancy
   - Stateless vs. Stateful Architecture
   - Azure Functions Triggers and Bindings

---

**Status:** ? **READY FOR DEPLOYMENT**

Last Updated: 2024
Project: AgriStreamIOT v1.0
