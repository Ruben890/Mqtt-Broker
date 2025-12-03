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
3. When the backend needs to notify a device, it publishes to `event/{chipId}`.
4. To send to a group, backend queries the DB for group members and publishes individually to each `event/{chipId}`.

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
	"Redis": {
		"Aof": "localhost:6379",
		"NoAof": "localhost:6380"
	}
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

Microcontroller implementation notes (ESP32/ESP8266 and OTA reference)
This section gives guidance for implementing the device-side client (microcontroller) compatible with this repo.

- Reference project for OTA over MQTT: https://github.com/JorgeGBeltre/MQTTOTA
	- That repo provides examples for ESP-based microcontrollers (ESP32/ESP8266) to receive firmware via MQTT and perform OTA updates.
	- Use it as a starting point to implement the device MQTT client, subscribe to `event/{chipId}` (where `chipId` matches device configuration), and handle `UPDATE_FIRMWARE` events.

- Device responsibilities:
	- On boot, connect to the MQTT broker and subscribe to `event/{chipId}` (and optionally `group/{groupName}` if you want group subscriptions on device side).
	- Parse incoming `MqttResponse<TDetails>` messages. For firmware updates, use `EventType == "UPDATE_FIRMWARE"` and `Details` payload to download and apply firmware (see MQTTOTA project for the OTA flow and file transfer over MQTT or via HTTP with URL provided).
	- Send status messages to backend by publishing `MqttRequest<TDetails>` to a configured backend topic (for example `status/{chipId}` or another agreed topic). In this repo the backend expects device-originated messages in the `MqttRequest` structure.

- Example device flow for firmware update using MQTTOTA as reference:
	1. Device subscribes to `event/{chipId}`.
	2. Backend publishes `MqttResponse<UpdateFirmwareMqtt>` with `EventType = "UPDATE_FIRMWARE"` and `Details` containing `version` and `url` (or chunks if using MQTT file transfer).
	3. Device receives message, validates version, downloads firmware (HTTP or MQTT stream) and performs OTA.
	4. Device publishes a status `MqttRequest<object>` indicating success/failure.

Integration tips:
- Align the `Details` DTOs between backend and device. If using JSON, ensure field names and types match exactly (case-sensitive depending on parser).
- Consider subscribing to `group/{groupId}` on-device if you want to allow broadcasting from backend without per-device publishes — but remember group membership should still be stored in DB for management and auditing.
- For minimal devices, implement only the necessary subset of `DeviceDto` (e.g. `ChipId`, `Name`, `FirmwareVersion`) when publishing status.

Recommendations and notes
- Use DTOs from `Src/Shared` to serialize/deserialize MQTT payloads and avoid schema mismatches.
- For local testing you can run `mosquitto` in Docker or any MQTT-compatible broker.
- Keep Redis configuration separate per environment. In production, review persistence, security and high-availability settings.

What can be changed
- Device grouping and topic conventions are extensible: you can rename topics (e.g. `event/` → `device/`) or create subtopics per event type.
- The `Shared.Request.MqttRequest` and `Shared.Response.MqttResponse` formats can be extended with extra fields — keep backward compatibility when possible.

Contact / Development
- Main project file: `Mqtt-Broker/Mqtt-Broker.csproj`.
- Configuration files: `Mqtt-Broker/appsettings.json` and `Mqtt-Broker/appsettings.Development.json`.

--
This README explains the architecture, project structure, how to run a development environment with two Redis instances (AOF true/false), and provides guidance for microcontroller implementation using the MQTTOTA project as a reference. If you want, I can:
- Add a full `docker-compose.yml` that runs the app plus both Redis instances for development.
- Extract DTO definitions into a small device-side JSON spec file and examples for common `Details` types (`UpdateFirmwareMqtt`, `TelemetryRecordRequest`, etc.).

