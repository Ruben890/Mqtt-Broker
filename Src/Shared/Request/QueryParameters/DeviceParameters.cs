using Shared.Enums;
using System.ComponentModel;
using Swashbuckle.AspNetCore.Annotations;

namespace Shared.Request.QueryParameters
{
    /// <summary>
    /// Search parameters for devices.
    /// </summary>
    public class DeviceParameters : RequestParameters
    {
        [SwaggerSchema("Filter by group name.")]
        [DefaultValue(null)]
        public string? GroupName { get; set; } = null!;

        [SwaggerSchema("Filter by device name.")]
        [DefaultValue(null)]
        public string? DeviceName { get; set; } = null!;

        [SwaggerSchema("Filter by firmware version.")]
        [DefaultValue(null)]
        public string? FirmwareVersion { get; set; } = null!;

        [SwaggerSchema("Filter by user name.")]
        [DefaultValue(null)]
        public string? UserName { get; set; } = null!;

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
    }
}
