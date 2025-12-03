using Shared.Utils;
using System.ComponentModel.DataAnnotations;

namespace Domain
{
    public abstract class EntityBase<TId>
    {
        [Key]
        [Required]
        public TId Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        protected EntityBase()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;

            Id = typeof(TId) switch
            {
                var t when t == typeof(Guid) => (TId)(object)UuidV7Generator.Create(),
                var t when t == typeof(int) => default!,
                _ => throw new NotSupportedException($"Tipo de ID no soportado: {typeof(TId).Name}")
            };
        }

        public void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;
    }
}
