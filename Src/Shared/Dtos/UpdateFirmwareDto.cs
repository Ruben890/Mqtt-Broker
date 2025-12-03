using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public class UpdateFirmwareDto
    {
        public Guid? Id { get; set; } = null!;
        public string? Dst { get; set; } = null!;
        public IFormFile? NewFirmwareFile { get; set; }

        public string ? FirmwareVersion { get; set; }

        [MaxLength(500)]
        public string? Feature { get; set; }

        public bool ActualVersion { get; set; } = false;

        [MaxLength(45)]
        public string? UpdatedFromIp { get; set; }

        public int? DevicesUsingFirmware {  get; set; }

        public DateTime? CreatedAt { get; set; } = null!;

    }
}
