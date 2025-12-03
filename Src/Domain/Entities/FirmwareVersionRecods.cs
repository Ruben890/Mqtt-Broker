using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class FirmwareVersionRecord : EntityBase<Guid>
    {
  
        [Required]
        [MaxLength(200)]
        public string Src { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Dst { get; set; }

        [MaxLength(500)]
        public string? Feature { get; set; }

        [Required]
        
        public string? FirmwareVersion { get; set; }

        public bool ActualVersion { get; set; }


        [MaxLength(45)] 
        public string? UpdatedFromIp { get; set; }

        [Required]
        [DefaultValue(false)]
        public bool isDelete { get; set; } = false;

    }
}
