using Shared.Enums;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel;
using System.Globalization;

namespace Shared.Request
{
    public class GenericParameters : RequestParameters
    {
        public Guid? GroupId { get; set; } = null!;
        
        public Guid? DeviceId { get; set; } = null!;

        public Guid? FirmwareRecordId { get; set; } = null!;

        public string? ApiVersion { get; set; }

        public string? RoleName { get; set; } = null!;

        [SwaggerSchema(
            Description = "Filter by device connection status. Possible values:\n" +
                          "Online = 0\n" +
                          "Offline = 1\n" +
                          "Connecting = 2\n" +
                          "Disconnected = 3\n" +
                          "Error = 4",
            Nullable = true
        )]
        [DefaultValue(null)]
        public ConnectStatus? Status { get; set; } = null!;

        private DateTime? _parsedDate;
        public string? Date
        {
            get
            {
                return _parsedDate?.ToString("d/M/yyyy, HH:mm:ss", CultureInfo.InvariantCulture);
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var formatosFecha = new[]
                    {
                        "d/M/yyyy, HH:mm:ss", // Formato original
                        "d-M-yyyy, HH:mm:ss", // Formato con guiones
                        "M/d/yyyy, HH:mm:ss", // Otro formato con orden diferente
                        "M-d-yyyy, HH:mm:ss", // Otro formato con guiones
                        "yyyy/MM/dd, HH:mm:ss", // Formato con año primero y barras
                        "yyyy-MM-dd, HH:mm:ss",
                        "yyyy-MM-dd"
                    };

                    try
                    {
                        _parsedDate = DateTime.ParseExact(value, formatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None);
                    }
                    catch (FormatException ex)
                    {
                        throw new FormatException("La fecha proporcionada no tiene un formato compatible.", ex);
                    }
                }
                else
                {
                    _parsedDate = null;
                }
            }
        }
        public DateTime? GetParsedDateTime() => _parsedDate;
        public TimeOnly? GetParsedTimeOnly()
        {
            if (_parsedDate.HasValue)
                return TimeOnly.FromDateTime(_parsedDate.Value);
            return null;
        }
    }
}
