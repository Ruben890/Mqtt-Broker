using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persitencia.Configurations
{
    public class DeviceConfiguration : IEntityTypeConfiguration<Device>
    {
        public void Configure(EntityTypeBuilder<Device> builder)
        {
            builder.ToTable(nameof(Device), schema:"IoT");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.MacAddress).IsRequired();
            builder.HasIndex(x => x.MacAddress).IsUnique();

            builder.Property(x => x.ChipId).IsRequired();
            builder.HasIndex(x => x.ChipId).IsUnique();

            builder.Property(x => x.Code).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();

            builder.HasOne(x => x.Groups)
                .WithMany(x => x.Devices)
                .HasForeignKey(x => x.GroupId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);


            builder.HasMany(x => x.TelemetryRecords)
             .WithOne(x => x.Device)
             .HasForeignKey(x => x.DeviceId)
             .OnDelete(DeleteBehavior.Cascade);



            builder.HasOne(ds => ds.Status)            
                    .WithOne(d => d.Device)             
                    .HasForeignKey<DeviceStatus>(ds => ds.DeviceId) 
                    .OnDelete(DeleteBehavior.Cascade);  
        }

    }
}
