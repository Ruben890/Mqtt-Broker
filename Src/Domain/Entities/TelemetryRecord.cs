using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table(nameof(TelemetryRecord), Schema = "IoT")]
    public class TelemetryRecord : EntityBase<Guid>
    {
        [Required]
        public Guid DeviceId { get; set; }

        public Device Device { get; set; } = null!;
    }
}
