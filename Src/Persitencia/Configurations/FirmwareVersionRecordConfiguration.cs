using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persitencia.Configurations
{
    public class FirmwareVersionRecordConfiguration : IEntityTypeConfiguration<FirmwareVersionRecord>
    {
        public void Configure(EntityTypeBuilder<FirmwareVersionRecord> builder)
        {
            builder.ToTable(nameof(FirmwareVersionRecord), schema: "IoT");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FirmwareVersion).IsRequired();

            builder.HasIndex(x => x.FirmwareVersion)
                .IsUnique();
        }
    }
}
