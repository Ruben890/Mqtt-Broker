# MQTT Broker - Clean Architecture

## Overview

Mqtt-Broker is an MQTT broker built on top of [MQTTnet](https://github.com/chkr1011/MQTTnet) with a focus on **IoT device management** and **Clean Architecture**. It provides:

* Per-device subscriptions (`event/{chipId}`)
* One-to-one and group event delivery
* Modular event handling strategies
* Database as source-of-truth for devices and groups
* Seamless .NET integration with DI, middleware, and REST APIs

---

## Architecture

### Main Layers

* **Presentation**: Controllers, middleware, REST endpoints.
* **Application**: Use cases, application services, AutoMapper profiles.
* **Domain**: Core business entities and logic.
* **Infrastructure**: MQTT, Redis, logging, etc.
* **Persistence**: Repositories, data contexts, UnitOfWork.
* **Shared**: DTOs, requests/responses, enums, utilities.
* **Extensions**: Configuration and service wiring helpers.

---

## MQTT Event Flow

1. **Client Connection**: MQTT service connects to the broker and subscribes to `event/{chipId}` for managed devices.
2. **Device Registration**: Device sends a `Status` event on connection. Backend registers or updates the device in the database.
3. **Individual Event Delivery**: Backend publishes to `event/{chipId}`.
4. **Group Event Delivery**: Backend queries DB for group members and publishes individually. Redis serves as cache/helper.

---

## Event Strategies

* **StatusStrategy**: Handles device registration and status updates.
* **TelemetryStrategy**: Processes telemetry data from devices.
* **ErrorUpdateFirmwareDeviceStrategy**: Logs errors during firmware updates.

---

## Message Structures

**MqttRequest<TDetails>** – from device to backend:

```json
{
  "Device": { "ChipId": "CHIP-12345", "Name": "Sensor-01", "FirmwareVersion": "1.2.3" },
  "Timestamp": "2025-12-03T12:00:00Z",
  "Details": { "temperature": 22.4 }
}
```

**MqttResponse<TDetails>** – from backend to device:

```json
{
  "EventType": "UPDATE_FIRMWARE",
  "Timestamp": "2025-12-03T12:05:00Z",
  "Details": { "version": "2.0.0", "url": "https://example.com/firmware.bin" }
}
```

---

## Device DTO

`DeviceDto` fields:

* `Id`, `ChipId`, `ChipType`, `Name`, `Code`, `MacAddress`, `FirmwareVersion`, `Status`, `GroupName`, `GroupDescription`, `Description`, `ErrMessage`

These structures support device status updates (`MqttRequest`) and backend commands/events (`MqttResponse`).

---

## Redis Configuration (Development)

Two Redis instances recommended:

* `redis-aof` – persistent (appendonly yes)
* `redis-noaof` – in-memory (appendonly no)

**Docker Run Example**:

```powershell
docker run -d --name redis-aof -p 6379:6379 redis:7 redis-server --appendonly yes
docker run -d --name redis-noaof -p 6380:6380 redis:7 redis-server --appendonly no
```

**Docker Compose Example**:

```yaml
version: '3.8'
services:
  redis-aof:
    image: redis:7
    command: ["redis-server", "--appendonly", "yes"]
    ports: ["6379:6379"]

  redis-noaof:
    image: redis:7
    command: ["redis-server", "--appendonly", "no"]
    ports: ["6380:6380"]
```

---

## Running the Project (Windows / PowerShell)

```powershell
dotnet restore "Mqtt-Broker.sln"
dotnet build "Mqtt-Broker.sln" -c Debug
# Start Redis instances
docker-compose -f docker-compose.redis.yml up -d
# Run application
dotnet run --project "Mqtt-Broker\Mqtt-Broker.csproj" -c Debug
```

---

## MQTT Topics & Event Routing

* **Device topic**: `event/{chipId}`
* **Group topic**: Optional `group/{groupId}`
* **Event type mapping**: Last segment of topic → `MqttEventType` (case-insensitive)

**Registered Event Types**:

* `Status` → `/status`
* `Telemetry` → `/telemetry`
* `ErrorUpdateFirmwareDevice` → `/ErrorUpdateFirmwareDevice`

**Status Event Flow**:

1. Device subscribes to `event/{chipId}`
2. Publishes `Status` message → backend maps topic segment → routes to `StatusStrategy`
3. `StatusStrategy` creates/updates device in DB and marks as online

---

## Microcontroller Notes (ESP32/ESP8266)

* Use [MQTTOTA](https://github.com/JorgeGBeltre/MQTTOTA) as reference for OTA updates.
* Device subscribes to `event/{chipId}` and optionally `group/{groupName}`.
* Sends Status events to register in backend.
* Handles incoming `MqttResponse<TDetails>` messages for firmware updates, commands, etc.

---

## API Keys, Middleware & Responses

* API keys for MQTT endpoints in `appsettings.json`
* Middleware handles:

  * Exception handling
  * Logging
  * CORS
  * API key validation
  * Standard response wrapper (`BaseResponse`)

**BaseResponse**:

* `Message` – string
* `StatusCode` – HTTP status
* `Details` – optional payload
* `Pagination` – optional metadata

---

## Security Notes

* Keep API keys secret; rotate regularly
* Production: prefer OAuth2/JWT over static keys

---

## Contact / Development

* Email: `chinausto@gmail.com`
* Main project file: `Mqtt-Broker/Mqtt-Broker.csproj`
* Config: `appsettings.json` / `appsettings.Development.json`

---
