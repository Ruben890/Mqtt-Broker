using Shared.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Domain.Entities
{

    [Table(nameof(DeviceStatus), Schema = "IoT")]
    public class DeviceStatus : EntityBase<Guid>
    {
        [Required]
        public Guid DeviceId { get; set; }

        public ConnectStatus Status { get; set; }

        [MaxLength(300)]
        [AllowNull]
        public string? ErrMenssage { get; set; } = null;

        [AllowNull]
        [DefaultValue(0)]
        public int LastFirmwareChunkSent { get; set; } = 0;

        public bool UpdateInProgress { get; set; } = false;

        public bool FirmwareUpdateCompleted { get; set; } = false;

        [MaxLength(50)]
        [AllowNull]
        public string? FirmwareVersionTarget { get; set; } = null;

        public Device Device { get; set; } = null!;

       
    }
}
