using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Domain.Entities
{
    [Table(nameof(Groups), Schema = "IoT")]
    public class Groups :EntityBase<Guid>
    {
        public Guid? UserId { get; set; } = null!;
        [Required]
        public string? Code { get; set; }
        [AllowNull]
        public string? Description { get; set; } = null;
        [Required]
        public string? GroupName { get; set; }

        [DefaultValue(false)]
        public bool IsActive { get; set; } = false;

        [DefaultValue(false)]
        public bool IsUnique { get; set; } = false;

        public Collection<Device>? Devices { get; set; } = null!;
    }
}
