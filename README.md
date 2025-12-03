**MQTT Broker - Clean Architecture**

- **Project:** MQTT Broker for IoT device management.
- **Architecture:** Clean Architecture (separated layers: Presentation, Application, Domain, Infrastructure, Persistence, Shared).

Summary:
- This repository implements an MQTT broker/service built on .NET (see `Mqtt-Broker` and the solution `Mqtt-Broker.sln`).
- It follows Clean Architecture principles to keep responsibilities separated, improve testability and scalability.
- Features: per-device subscription using the `event/{chipId}` topic, one-to-one event delivery, grouping of devices, and group event delivery.

Project structure
- `Mqtt-Broker/`: Main application (API/host). Contains `Program.cs`, `appsettings.json` and server configuration.
- `Src/Presentacion/`: Controllers, middleware and exposed endpoints.
- `Src/Application/`: Use cases, application services and mappings (AutoMapper profiles in `Mappers`).
- `Src/Domain/`: Domain entities and business logic.
- `Src/Infrastructure/`: Concrete implementations for MQTT, Redis, logging, etc.
- `Src/Persitencia/`: Repositories, data contexts and UnitOfWork.
- `Src/Shared/`: DTOs, standard Requests/Responses (`Shared.Request.MqttRequest`, `Shared.Response.MqttResponse`), enums and utilities.
- `Extencions/`: Extension methods for configuration and service wiring.

Main goals
- Serve as a central point to receive and route MQTT events to/from devices.
- Allow per-device subscription using the `event/{chipId}` topic so devices can be targeted one-to-one.
- Support grouping devices so the backend can send events to many devices at once.

MQTT topics and grouping (DB source-of-truth)
- `event/{chipId}`: topic for events targeted at device with `chipId`. The MQTT client/service should subscribe to this topic after connecting to receive messages for that device.

Important note about grouping: device grouping is managed primarily at the database level (stored in the devices table/collection or a relation table `group -> devices`). When sending an event to a group, the backend queries the database to obtain the list of devices associated with the `groupId` and then publishes to `event/{chipId}` for each device. The group-send flow is:

1. Backend receives a request to send an event to `groupId`.
2. Backend queries the database (Repository/UnitOfWork) to get the `chipId`s registered in that group.
3. Backend publishes a message to `event/{chipId}` for each `chipId` (one-to-one publish). Optionally, if clients subscribe to a `group/{groupId}` topic, backend may publish to `group/{groupId}`.

This ensures the database is the source of truth for group membership. Redis is used as a helper (cache, fast mappings), but the authoritative source is the DB.

Basic event flow (receive & send)
1. The MQTT service (client of the broker) connects to the MQTT broker.
2. After connecting, the service subscribes to `event/{chipId}` topics for the devices it manages (e.g., all active devices from the DB it needs to monitor).
3. **Device registration**: After subscribing to `event/{chipId}`, the device **must send a Status event** (via MQTT message with EventType or status topic) to the backend. The backend's `StatusStrategy` receives this message, extracts device info, and registers/updates the device in the database. This ensures the device is discoverable and the system can send/receive events to/from it.
4. When the backend needs to notify a device, it publishes to `event/{chipId}` (device must be registered first, per step 3).
5. To send to a group, backend queries the DB for group members and publishes individually to each `event/{chipId}`.

Message standards (real definitions in code)
The real classes live in `Src/Shared` and use generics for `Details`:

- `Shared.Request.MqttRequest<TDetails>` (see `Src/Shared/Request/MqttRequest.cs`):
	- `Device` (type `DeviceDto`): basic device information sending the message.
	- `Timestamp` (DateTime): UTC timestamp for the message.
	- `Details` (TDetails): message-specific payload.

- `Shared.Response.MqttResponse<TDetails>` (see `Src/Shared/Response/MqttResponse.cs`):
	- `EventType` (string): event type (e.g. `REBOOT`, `UPDATE_FIRMWARE`).
	- `Timestamp` (DateTime): UTC timestamp for the event.
	- `Details` (TDetails): additional data for the event.

`DeviceDto` (real fields)
The messages include a `DeviceDto` with these fields (defined in `Src/Shared/Dtos/DeviceDto.cs`):
- `Id` (Guid?)
- `GroupName` (string?)
- `GroupDescription` (string?)
- `Status` (string?)
- `MacAddress` (string?)
- `ChipId` (string?)
- `ChipType` (string?)
- `Name` (string?)
- `Code` (string?)
- `Description` (string?)
- `FirmwareVersion` (string?)
- `ErrMessage` (string?)

These structures allow devices to send status (`MqttRequest`) and backend to send commands/events (`MqttResponse`).

Concrete JSON examples
`MqttRequest<object>` (payload sent by a device to backend):

```json
{
	"Device": {
		"Id": "d290f1ee-6c54-4b01-90e6-d701748f0851",
		"GroupName": "warehouses",
		"GroupDescription": "Devices in warehouses",
		"Status": "Online",
		"MacAddress": "00:1A:7D:DA:71:13",
		"ChipId": "CHIP-12345",
		"ChipType": "ESP32",
		"Name": "Sensor-01",
		"Code": "SEN-01",
		"Description": "Warehouse temperature sensor",
		"FirmwareVersion": "1.2.3",
		"ErrMessage": null
	},
	"Timestamp": "2025-12-03T12:00:00Z",
	"Details": {
		"temperature": 22.4,
		"battery": 87
	}
}
```

`MqttResponse<UpdateFirmwareMqtt>` (message sent by backend to start firmware update):

```json
{
	"EventType": "UPDATE_FIRMWARE",
	"Timestamp": "2025-12-03T12:05:00Z",
	"Details": {
		"version": "2.0.0",
		"url": "https://artifacts.example.com/firmware/2.0.0.bin"
	}
}
```

Note: concrete `Details` types (e.g. `UpdateFirmwareMqtt`, `TelemetryRecordRequest`, etc.) are defined in `Src/Shared/Dtos/MqttRequest` and `Src/Shared/Dtos/MqttResponse`.

Redis configuration (development)
- The project uses Redis for caching/state and to help manage group/device mappings. For local development we recommend running two Redis instances:
	- `redis-aof` with AOF enabled (`appendonly yes`) — AOF persistence.
	- `redis-noaof` with AOF disabled (`appendonly no`) — no AOF, useful for fast tests.

PowerShell example to start two instances (Windows + Docker Desktop):

```pwsh
# AOF = true on 6379
docker run -d --name redis-aof -p 6379:6379 redis:7 redis-server --appendonly yes

# AOF = false on 6380
docker run -d --name redis-noaof -p 6380:6380 redis:7 redis-server --appendonly no
```

Docker Compose example (`docker-compose.redis.yml`):

```yaml
version: '3.8'
services:
	redis-aof:
		image: redis:7
		command: ["redis-server", "--appendonly", "yes"]
		ports:
			- "6379:6379"

	redis-noaof:
		image: redis:7
		command: ["redis-server", "--appendonly", "no"]
		ports:
			- "6380:6380"
```

Start commands (PowerShell):

```pwsh
# With docker-compose
docker-compose -f docker-compose.redis.yml up -d

# Or with docker run
docker run -d --name redis-aof -p 6379:6379 redis:7 redis-server --appendonly yes
docker run -d --name redis-noaof -p 6380:6380 redis:7 redis-server --appendonly no
```

`appsettings.json` example

```json
{
  "ConnectionStrings": {
    "MqttBrokerConnection": "Server=***;Port=5432;Username=***;Password=***;Database=***;CommandTimeout=300;Pooling=true;MaxPoolSize=100;MinPoolSize=5;ConnectionIdleLifetime=300;Include Error Detail=true"
  },
  "ApiKeys": "***",
  "Redis": {
    "Persistent": {
      "ConnectionString": "localhost:6378",
      "UseAOF": true
    },
    "InMemory": {
      "ConnectionString": "localhost:6379",
      "UseAOF": false
    }
  },
  "MqttSettings": {
    "Port": 8083,
    "ApiKeys": [
      { "key1": "***" }
    ]
  },
  "ExcludedPaths": [
    "/scalar/*",
    "/swagger/*",
    "/favicon.ico"
  ],
  "AllowedOrigins": [
    "http://localhost:5000",
    "https://localhost:5000",
    "http://localhost:5173",
    "https://localhost:5173"
  ]
}
```

How to run the project (Windows / PowerShell)
1. Restore packages and build the solution:

```pwsh
dotnet restore "Mqtt-Broker.sln"
dotnet build "Mqtt-Broker.sln" -c Debug
```

2. Start Redis instances (see section above).

3. Run the application (from solution or project):

```pwsh
dotnet run --project "Mqtt-Broker\Mqtt-Broker.csproj" -c Debug
```

4. Check logs and endpoints in `appsettings.Development.json` / `appsettings.json`.

MQTT connection and subscriptions
- On startup, the MQTT service should connect to the configured MQTT broker (e.g. Mosquitto, EMQX, or an embedded broker).
- After connecting, subscribe to `event/{chipId}` for each device the service manages.
- Only clients subscribed to `event/{chipId}` will receive messages published on that topic — this enables one-to-one delivery.

Example publish (send an event to device `12345`):

```pwsh
# Publish JSON message to topic event/12345
mqtt-pub -h <broker_host> -t "event/12345" -m '{ "Device": { "ChipId":"12345" }, "Timestamp":"2025-12-03T12:00:00Z", "Details": { "status":"ok" }}'
```

Device registration and StatusStrategy
After a device connects and subscribes to `event/{chipId}`, it **must send a Status message** to register itself in the system. This is crucial for the backend to know the device exists and can communicate.

Flow:
1. Device connects to broker and subscribes to `event/{chipId}`.
2. Device publishes a `MqttRequest<object>` or status message containing its device info (e.g., `ChipId`, `Name`, `FirmwareVersion`, etc.) to a status topic (e.g. `status/{chipId}` or directly to the broker using the `MqttRequest` format).
3. Backend's `StatusStrategy` (see `Src/Infrastructure/Mqtt/MqttStrategies/StatusStrategy.cs`) receives the status message, parses the `MqttRequest<object>`, extracts the `Device` object, and:
   - Creates or updates the device record in the database.
   - Sets device status to `Online`.
   - Records the timestamp.
4. Once registered, the device can receive events published to `event/{chipId}` and can be targeted by group operations.

Example Status message (device sending registration):

```json
{
  "Device": {
    "ChipId": "CHIP-12345",
    "ChipType": "ESP32",
    "Name": "Sensor-01",
    "Code": "SEN-01",
    "MacAddress": "00:1A:7D:DA:71:13",
    "FirmwareVersion": "1.2.3",
    "Status": "Online"
  },
  "Timestamp": "2025-12-03T12:00:00Z",
  "Details": null
}
```

**Note**: The status message should be published on the same MQTT broker or to a topic that the backend subscribes to. The exact topic depends on your backend configuration (e.g., `status/{chipId}`, `devices/register`, or another topic monitored by the MQTT infrastructure).

Microcontroller implementation notes (ESP32/ESP8266 and OTA reference)
This section gives guidance for implementing the device-side client (microcontroller) compatible with this repo.

- Reference project for OTA over MQTT: https://github.com/JorgeGBeltre/MQTTOTA
	- That repo provides examples for ESP-based microcontrollers (ESP32/ESP8266) to receive firmware via MQTT and perform OTA updates.
	- Use it as a starting point to implement the device MQTT client, subscribe to `event/{chipId}` (where `chipId` matches device configuration), and handle `UPDATE_FIRMWARE` events.

- Device responsibilities:
	- On boot, connect to the MQTT broker and subscribe to `event/{chipId}` (and optionally `group/{groupName}` if you want group subscriptions on device side).
	- **Immediately after subscribing**, publish a Status message (with device info) so the backend's `StatusStrategy` registers the device in the DB.
	- Parse incoming `MqttResponse<TDetails>` messages. For firmware updates, use `EventType == "UPDATE_FIRMWARE"` and `Details` payload to download and apply firmware (see MQTTOTA project for the OTA flow and file transfer over MQTT or via HTTP with URL provided).
	- Send status/telemetry messages periodically or on-demand to keep the device registration active. In this repo the backend expects device-originated messages in the `MqttRequest` structure.

- Example device flow for firmware update using MQTTOTA as reference:
	1. Device subscribes to `event/{chipId}`.
	2. Backend publishes `MqttResponse<UpdateFirmwareMqtt>` with `EventType = "UPDATE_FIRMWARE"` and `Details` containing `version` and `url` (or chunks if using MQTT file transfer).
	3. Device receives message, validates version, downloads firmware (HTTP or MQTT stream) and performs OTA.
	4. Device publishes a status `MqttRequest<object>` indicating success/failure.

Integration tips:
- Align the `Details` DTOs between backend and device. If using JSON, ensure field names and types match exactly (case-sensitive depending on parser).
- Consider subscribing to `group/{groupId}` on-device if you want to allow broadcasting from backend without per-device publishes — but remember group membership should still be stored in DB for management and auditing.
- For minimal devices, implement only the necessary subset of `DeviceDto` (e.g. `ChipId`, `Name`, `FirmwareVersion`) when publishing status.

**API Keys, Middleware and API response standard**

The project expects API key configuration and registers middleware to handle cross-cutting concerns (CORS, API key validation, exception handling, and a standard response wrapper). Below is a guide to the configuration and the runtime behavior.

`appsettings.json` snippet (see above) includes:
- `ConnectionStrings.MqttBrokerConnection`: DB connection string (Postgres example).
- `ApiKeys`: global API key string (legacy/simple usage).
- `MqttSettings.ApiKeys`: array of keys used specifically for MQTT endpoints (can be validated by middleware).
- `ExcludedPaths`: list of paths excluded from some middlewares (e.g. static files, swagger).
- `AllowedOrigins`: CORS allowed origins.

Suggested middleware pipeline (order matters):
1. **Exception handling**: global exception handler to return `BaseResponse` on unhandled errors.
2. **Logging**: request/response logging.
3. **CORS**: configured using `AllowedOrigins`.
4. **ApiKey validation**: middleware that checks requests against configured API keys (either `ApiKeys` or `MqttSettings.ApiKeys`). Returns `401`/`403` wrapped in `BaseResponse` on failure.
5. **Authentication/Authorization**: JWT or other schemes if required.
6. **StandardResponseFilter**: an MVC action filter (`Presentacion.Filters.StandardResponseFilter`) that ensures every controller action returns a `BaseResponse`-shaped payload. It also converts exceptions into `BaseResponse`.

Standard API response (`BaseResponse`) — real code (see `Src/Shared/Response/BaseResponse.cs`):
- `Message` (string?) — localized message or summary.
- `StatusCode` (HttpStatusCode) — the HTTP status.
- `Details` (object?) — optional payload with data or error details.
- `Pagination` (Pagination?) — optional pagination meta when returning lists.

Utility: `BaseResponse.HandleCustomResponse(message, statusCode, details, pagination)` returns a properly formed `BaseResponse` and sets `Pagination` when provided.

Example controller usage (C# pseudo-code):

```csharp
[HttpGet("devices/{groupId}")]
public async Task<IActionResult> GetDevicesByGroup(Guid groupId)
{
    var devices = await _deviceService.GetDevicesByGroupAsync(groupId);
    var response = BaseResponse.HandleCustomResponse(
        message: "devices_fetched",
        statusCode: HttpStatusCode.OK,
        details: devices
    );
    return Ok(response); // StandardResponseFilter will respect BaseResponse
}
```

Example JSON response (success):

```json
{
  "Message": "Operation completed successfully.",
  "StatusCode": 200,
  "Details": [ { "ChipId": "CHIP-12345", "Name": "Sensor-01" } ],
  "Pagination": null
}
```

Example JSON response (error):

```json
{
  "Message": "An unexpected error occurred.",
  "StatusCode": 500,
  "Details": "NullReferenceException: ...",
  "Pagination": null
}
```

Notes about API key middleware:
- Implement a small middleware that reads `Authorization` header or a custom header (e.g. `X-Api-Key`) and compares against `MqttSettings.ApiKeys` or `ApiKeys`.
- If the request path matches an `ExcludedPaths` pattern, skip API key validation.
- On invalid key, return `401`/`403` with a `BaseResponse` payload.

Example minimal ApiKey middleware (conceptual):

```csharp
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (IsExcluded(path))
        {
            await _next(context);
            return;
        }

        var key = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        var configuredKeys = _config.GetSection("MqttSettings:ApiKeys").Get<List<Dictionary<string,string>>>()
            .SelectMany(d => d.Values).ToList();

        if (string.IsNullOrEmpty(key) || !configuredKeys.Contains(key))
        {
            context.Response.StatusCode = 401;
            var resp = BaseResponse.HandleCustomResponse("Invalid API Key", HttpStatusCode.Unauthorized);
            await context.Response.WriteAsJsonAsync(resp);
            return;
        }

        await _next(context);
    }
}
```

Security notes:
- Keep API keys secret and rotate them as needed.
- For production, prefer a full auth solution (OAuth2/JWT) over static API keys.

Contact / Development
- Main project file: `Mqtt-Broker/Mqtt-Broker.csproj`.
- Configuration files: `Mqtt-Broker/appsettings.json` and `Mqtt-Broker/appsettings.Development.json`.

--
This README explains the architecture, project structure, how to run a development environment with two Redis instances (AOF true/false), provides guidance for microcontroller implementation using the MQTTOTA project as a reference, and documents API key configuration, middleware pipeline and the `BaseResponse` API standard. If you want, I can:
- Add a full `docker-compose.yml` that runs the app plus both Redis instances for development.
- Extract DTO definitions into a small device-side JSON spec file and examples for common `Details` types (`UpdateFirmwareMqtt`, `TelemetryRecordRequest`, etc.).


