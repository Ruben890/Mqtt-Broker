using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Persitencia.Contexts
{
    public class MqttBrokerContext : DbContext
    {
        private readonly DbContextOptions<MqttBrokerContext> _options;

        public MqttBrokerContext(DbContextOptions<MqttBrokerContext> options) : base(options)
        {
            _options = options;
        }

        public DbContextOptions<MqttBrokerContext> GetDbContextOptions() => _options;
        public DbSet<FirmwareVersionRecord> FirmwareVersionRecords { get; set; }
        public DbSet<DeviceStatus> DeviceStatuses { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Groups> Groups { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }
    }
}
