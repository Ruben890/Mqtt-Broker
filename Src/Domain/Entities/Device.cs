using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Domain.Entities
{
    public class Device : EntityBase<Guid>
    {

        [AllowNull]
        public Guid? GroupId { get; set; } = null!;

        [Required]
        public string MacAddress { get; set; } = null!;

        [Required]
        public string? ChipId { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string ChipType { get; set; } = null!;

        [Required] [StringLength(50)]
        public string? FirmwareVersion { get; set; }

        [AllowNull]
        [StringLength(100)]
        public string? IdentificationName { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = null!;

        [AllowNull]
        [StringLength(250)]
        public string? Description { get; set; }

        public Groups? Groups { get; set; } = null!;

        public DeviceStatus? Status { get; set; } = null!;
        public Collection<TelemetryRecord> TelemetryRecords { get; set; } = null!;

    }
}
