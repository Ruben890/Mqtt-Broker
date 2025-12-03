using Application.Contract.IMqtt;
using Application.Contract.IServcies;
using Application.Contract.IUnitOfWork;
using Application.Contracts;
using AutoMapper;
using Domain.Entities;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shared.Dtos;
using Shared.Dtos.MqttResponse;
using Shared.Enums;
using Shared.Request;
using Shared.Response;
using System.Net;
using static Shared.Response.BaseResponse;

namespace Application.Services
{
    public class FirmwareServices : IFirmwareServices
    {
        private readonly IMqttBrokerUnitOfWorkManager _repository;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerManager<FirmwareServices> _logger;
        private readonly IMapper _mapper;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly IMqttServerService _mqttServerService;


        public FirmwareServices(
            IMqttBrokerUnitOfWorkManager repository,
            IServiceProvider serviceProvider,
            IMqttServerService mqttServerService,
            IMapper mapper,
            IBackgroundJobClient backgroundJobs,
            ILoggerManager<FirmwareServices> logger
        )
        {
            _mapper = mapper;
            _serviceProvider = serviceProvider;
            _mqttServerService = mqttServerService;
            _logger = logger;
            _repository = repository;
            _backgroundJobs = backgroundJobs;
        }



        public async Task<BaseResponse> UpdateFirmwareVersion(GenericParameters parameters, UpdateFirmwareDto request)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(request.UpdatedFromIp))
                    return HandleCustomResponse("The 'UpdatedFromIp' field is required and cannot be empty.", HttpStatusCode.BadRequest);

                if (request.NewFirmwareFile == null)
                    return HandleCustomResponse("A firmware file must be provided.", HttpStatusCode.BadRequest);

                if (string.IsNullOrWhiteSpace(request.FirmwareVersion))
                    return HandleCustomResponse("The 'FirmwareVersion' value is required.", HttpStatusCode.BadRequest);

                var versionString = request.FirmwareVersion.Trim().TrimStart('v', 'V');

                if (!Version.TryParse(versionString, out var version) || version <= new Version(0, 0, 0))
                    return HandleCustomResponse("The 'FirmwareVersion' value must be a valid version greater than 0.0.0.", HttpStatusCode.BadRequest);


                if(string.IsNullOrWhiteSpace(parameters.GroupId.ToString()) || parameters.GroupId == Guid.Empty)
                    return HandleCustomResponse("The 'GroupId' parameter is required and cannot be empty.", HttpStatusCode.BadRequest);

                // --- GUARDAR REGISTRO DE FIRMWARE (rápido) ---
                await _repository.BeginAsync();

                await HandleFirmwareVersionAsync(request);

                await _repository.SaveAsync();

                await _repository.CommitAsync();

                // === DISPARAR EL JOB (DI, SIN Task.Run) ===
                _logger.LogInfo($"Dispatching Firmware Update Job for version {request.FirmwareVersion}");

                // Opción A: Si _job es una instancia inyectada
                _backgroundJobs.Enqueue(() => UpdateFirmware(
                    parameters,
                    request.FirmwareVersion,
                    CancellationToken.None
                ));

                // --- RESPUESTA INMEDIATA AL CLIENTE ---
                return HandleCustomResponse(
                    "Firmware version registered successfully. Update process has started in background.",
                    HttpStatusCode.Accepted
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message, ex);
                await _repository.RollbackAsync();
                return HandleCustomResponse(ex.InnerException?.Message ?? ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public async Task<BaseResponse> RollbackFirmwareVersion(GenericParameters parameters)
        {
            try
            {
                if (parameters.FirmwareRecordId == null || parameters.FirmwareRecordId == Guid.Empty)
                {
                    return HandleCustomResponse(
                        "The 'FirmwareRecordId' field is required and cannot be empty.",
                        HttpStatusCode.BadRequest);
                }

                if (string.IsNullOrWhiteSpace(parameters.GroupId.ToString()) || parameters.GroupId == Guid.Empty)
                    return HandleCustomResponse("The 'GroupId' parameter is required and cannot be empty.", HttpStatusCode.BadRequest);

                await _repository.BeginAsync();

                var existingRecord = await _repository.FirmwareVersionRecordRepository
                    .GetFirmwareById(parameters.FirmwareRecordId.Value);

                if (existingRecord == null)
                {
                    return HandleCustomResponse(
                        $"Firmware record with ID {parameters.FirmwareRecordId} was not found.",
                        HttpStatusCode.NotFound);
                }


                // Check if the Src file exists
                if (string.IsNullOrWhiteSpace(existingRecord.Src) || !File.Exists(existingRecord.Src))
                {
                    return HandleCustomResponse(
                        $"Firmware file at '{existingRecord.Src}' does not exist.",
                        HttpStatusCode.BadRequest);
                }

                // === DISPARAR EL JOB (DI, SIN Task.Run) ===
                _logger.LogInfo($"Dispatching Firmware Rollback Job for version {existingRecord.FirmwareVersion}");

                _backgroundJobs.Enqueue(() => UpdateFirmware(
                    parameters,
                    existingRecord.FirmwareVersion!,
                    CancellationToken.None
                ));

                await _repository.SaveAsync();
                await _repository.CommitAsync();

                return HandleCustomResponse("Firmware rollback completed successfully.", HttpStatusCode.Accepted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message, ex);
                await _repository.RollbackAsync();
                return HandleCustomResponse(
                    ex.InnerException?.Message ?? ex.Message,
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<BaseResponse> GetFirmwareVersionRecords(GenericParameters parameters)
        {
            var firmwareVersionRecords = await _repository.FirmwareVersionRecordRepository.GetFirmwareVersionRecords(parameters);

            if (firmwareVersionRecords == null || !firmwareVersionRecords.Any())
            {
                // No firmware versions found
                return HandleCustomResponse("No firmware versions were found matching the provided parameters.", HttpStatusCode.NotFound);
            }

            // Successfully retrieved firmware versions
            return HandleCustomResponse("Firmware versions retrieved successfully.", HttpStatusCode.OK, firmwareVersionRecords, firmwareVersionRecords.Pagination);
        }

        private async Task HandleFirmwareVersionAsync(UpdateFirmwareDto request)
        {
            var firmwareRecord = _mapper.Map<FirmwareVersionRecord>(request);

            // Check if this firmware version already exists
            var existingRecord = await _repository.FirmwareVersionRecordRepository
                .GetFirmwareByVersion(firmwareRecord.FirmwareVersion!);

            if (existingRecord != null)
            {
                await DeleteExistingFirmwareAsync(existingRecord);
            }

            await SaveNewFirmwareAsync(request, firmwareRecord);
        }

        private async Task DeleteExistingFirmwareAsync(FirmwareVersionRecord existingRecord)
        {
            // Delete the physical file (Src)
            if (!string.IsNullOrWhiteSpace(existingRecord.Src) && File.Exists(existingRecord.Src))
            {
                try
                {
                    File.Delete(existingRecord.Src);
                    _logger.LogInfo($"File deleted: {existingRecord.Src}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error deleting file {existingRecord.Src}: {ex.Message}");
                }
            }

            // Remove the existing record from the repository
            _repository.FirmwareVersionRecordRepository.RemoveFirmwareVersionRecord(existingRecord);
            await _repository.SaveAsync();

            _logger.LogInfo($"Firmware version {existingRecord.FirmwareVersion} record removed.");
        }

        private async Task SaveNewFirmwareAsync(UpdateFirmwareDto request, FirmwareVersionRecord firmwareRecord)
        {
            // Validar que se haya enviado un archivo
            if (request.NewFirmwareFile is null || request.NewFirmwareFile.Length == 0)
            {
                _logger.LogWarn("No firmware file was provided.");
                throw new InvalidOperationException("Firmware file is required.");
            }

            // Obtener la extensión del archivo
            var fileExtension = Path.GetExtension(request.NewFirmwareFile.FileName)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                _logger.LogWarn("Firmware file has no extension.");
                throw new InvalidOperationException("Firmware file must have an extension.");
            }

            // Lista de extensiones permitidas
            var allowedExtensions = new[] { ".bin", ".hex", ".dfu" };
            if (!allowedExtensions.Contains(fileExtension))
            {
                _logger.LogWarn($"Firmware file extension '{fileExtension}' is not allowed.");
                throw new InvalidOperationException($"Firmware file type '{fileExtension}' is not permitted.");
            }

            // Crear carpeta destino
            var folderPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "firmware", request.FirmwareVersion!);
            Directory.CreateDirectory(folderPath);

            // Construir ruta completa incluyendo extensión
            var fileFullPath = Path.Combine(folderPath, $"{request.FirmwareVersion}{fileExtension}");

            // Guardar el archivo
            using (var stream = new FileStream(fileFullPath, FileMode.Create))
            {
                await request.NewFirmwareFile.CopyToAsync(stream);
            }

            _logger.LogInfo($"New firmware file saved at: {fileFullPath}");

            // Guardar ruta relativa y metadata en la base de datos
            firmwareRecord.Src = Path.Combine("firmware", request.FirmwareVersion!, $"{request.FirmwareVersion}{fileExtension}");
            firmwareRecord.CreatedAt = DateTime.UtcNow;

            await _repository.FirmwareVersionRecordRepository.AddFirmwareVersionRecord(firmwareRecord);
            await _repository.SaveAsync();

            _logger.LogInfo($"Firmware version {firmwareRecord.FirmwareVersion} record added.");
        }

        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        [DisableConcurrentExecution(timeoutInSeconds: 3600)]
        public async Task UpdateFirmware(GenericParameters parameters, string version, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(version)) return;

            // 1. Obtener info del Firmware
            FirmwareVersionRecord? firmwareRecord;
            using (var scopeRead = _serviceProvider.CreateAsyncScope())
            {
                var uow = scopeRead.ServiceProvider.GetRequiredService<IMqttBrokerUnitOfWorkManager>();
                firmwareRecord = await uow.FirmwareVersionRecordRepository.GetFirmwareByVersion(version);
            }

            if (firmwareRecord == null)
            {
                _logger.LogError($"Firmware version {version} not found in DB.");
                return;
            }

            // 2. Validar archivo
            var srcFull = Path.Combine(AppContext.BaseDirectory, "wwwroot", firmwareRecord.Src);
            if (!File.Exists(srcFull))
            {
                _logger.LogError($"Firmware file not found at {srcFull}");
                return;
            }

            // 3. Preparar datos
            const int chunkSizeBytes = 2 * 1024;
            const int maxRetries = 3;
            byte[] firmwareBytes = await File.ReadAllBytesAsync(srcFull, cancellationToken);
            int totalParts = (int)Math.Ceiling((double)firmwareBytes.Length / chunkSizeBytes);

            // 4. Obtener IDs de dispositivos
            List<string?>? deviceChipIds;
            using (var scopeList = _serviceProvider.CreateAsyncScope())
            {
                var uowList = scopeList.ServiceProvider.GetRequiredService<IMqttBrokerUnitOfWorkManager>();
                deviceChipIds = await uowList.DeviceRepository.GetDeviceChidIds(parameters);
            }

            _logger.LogInfo($"Found {deviceChipIds.Count()} devices to update.");

            // 5. Iterar dispositivos
            foreach (var chipId in deviceChipIds)
            {
                if (cancellationToken.IsCancellationRequested) break;

                using (var scopeProcess = _serviceProvider.CreateAsyncScope())
                {
                    var uowProcess = scopeProcess.ServiceProvider.GetRequiredService<IMqttBrokerUnitOfWorkManager>();

                    try
                    {
                        var device = await uowProcess.DeviceRepository.GetDeviceByChipId(chipId!);
                        if (device == null || device.Status == null) continue;

                        var status = device.Status;

                        // Saltar si ya completó
                        if (status.LastFirmwareChunkSent >= totalParts && status.UpdateInProgress)
                        {
                            _logger.LogInfo($"Device {device.ChipId} already updating. Skipping.");
                            continue;
                        }

                        // Saltar si ya completó
                        if (status.FirmwareUpdateCompleted)
                        {
                            _logger.LogInfo($"Device {device.ChipId} already completed firmware. Skipping.");
                            continue;
                        }

                        // Normalize versions: remove leading 'v' or 'V' and trim spaces
                        var existingVersion = device.FirmwareVersion?.Trim().TrimStart('v', 'V');
                        var newVersion = firmwareRecord.FirmwareVersion?.Trim().TrimStart('v', 'V');

                        // Compare ignoring case and leading 'v'
                        if (existingVersion == newVersion)
                        {
                            _logger.LogInfo($"Device {device.ChipId} already has firmware version {newVersion}. Skipping.");
                            continue;
                        }

                        _logger.LogInfo($"Starting firmware update for device: {device.ChipId}");


                        // Marcar inicio de actualización
                        await _repository.BeginAsync();

                        status.UpdateInProgress = true;
                        status.FirmwareVersionTarget = version;
                        await uowProcess.SaveAsync();

                        int startChunk = status.LastFirmwareChunkSent + 1;

                        _logger.LogInfo($"Device {device.ChipId}: starting from chunk {startChunk} of {totalParts}");

                        // Enviar chunks desde donde se quedó
                        for (int partIndex = startChunk; partIndex <= totalParts; partIndex++)
                        {
                            int offset = (partIndex - 1) * chunkSizeBytes;
                            int bytesToSend = Math.Min(chunkSizeBytes, firmwareBytes.Length - offset);
                            string base64Part = Convert.ToBase64String(firmwareBytes, offset, bytesToSend);

                            var mqttResponse = new MqttResponse<UpdateFirmwareMqtt>
                            {
                                EventType = EventType.UpdateFirmwareDevice.ToString(),
                                Timestamp = DateTime.UtcNow,
                                Details = new UpdateFirmwareMqtt
                                {
                                    FirmwareVersion = version,
                                    Base64Part = base64Part,
                                    PartIndex = partIndex,
                                    TotalParts = totalParts
                                }
                            };

                            var message = JsonConvert.SerializeObject(mqttResponse);

                            bool sent = await SendMqttWithRetryAsync(device.ChipId!, message, maxRetries);

                            if (!sent)
                            {
                                _logger.LogWarn($"Device {device.ChipId}: failed to send chunk {partIndex}. Will retry next job run.");
                                break; // Salir del loop, el próximo job continuará desde aquí
                            }

                            // Guardar progreso
                            status.LastFirmwareChunkSent = partIndex;
                            await uowProcess.SaveAsync();

                            await Task.Delay(250, cancellationToken);
                        }

                        // Verificar si se completó
                        if (status.LastFirmwareChunkSent >= totalParts)
                        {
                            device.FirmwareVersion = version;
                            status.UpdateInProgress = false;
                            status.FirmwareUpdateCompleted = true;
                            await uowProcess.SaveAsync();

                            _logger.LogInfo($"Device {device.ChipId}: firmware update completed successfully.");
                        }
                        else
                        {
                            _logger.LogInfo($"Device {device.ChipId}: firmware update paused at chunk {status.LastFirmwareChunkSent} of {totalParts}. Will continue on next job run.");
                        }
                        
                        await _repository.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await _repository.RollbackAsync();
                        _logger.LogError($"Error updating device {chipId}: {ex.Message}");
                    }
                }
            }
        }


        // Helper para limpiar el código principal
        private async Task<bool> SendMqttWithRetryAsync(string chipId, string message, int maxRetries)
        {
            int attempt = 0;
            while (attempt < maxRetries)
            {
                attempt++;
                if (await _mqttServerService.SendEventToUserByChipIdAsync(chipId, message)) return true;
                await Task.Delay(200);
            }
            return false;
        }

    }
}
