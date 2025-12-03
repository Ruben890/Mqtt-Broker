using Shared.Enums;

namespace Shared.Dtos
{
    public class DeviceDto
    {
        public Guid? Id { get; set; } = null!;
        public string? GroupName { get; set; } = null!;
        public string? GroupDescription { get; set; } = null!;
        public string? Status { get; set; } = ConnectStatus.Offline.ToString();
        public string? MacAddress { get; set; }
        public string? ChipId { get; set; }
        public string? ChipType { get; set; } = null;
        public string? Name { get; set; } = null!;
        public string? Code { get; set; }
        public string? Description { get; set; } = null!;
        public string? FirmwareVersion { get; set; }
        public string? ErrMessage { get; set; } = null!;
    }

}
