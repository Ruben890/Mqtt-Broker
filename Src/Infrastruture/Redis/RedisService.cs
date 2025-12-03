using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Infrastructure.Redis
{
    public class RedisService : IRedisService
    {
        private readonly IConfiguration _configuration;
        private readonly Lazy<IConnectionMultiplexer> _persistentRedis;
        private readonly Lazy<IConnectionMultiplexer> _inMemoryRedis;

        public RedisService(IConfiguration configuration)
        {
            _configuration = configuration;

            // Lazy para conectar solo cuando se necesite
            _persistentRedis = new Lazy<IConnectionMultiplexer>(() =>
            {
                var connectionString = _configuration.GetSection("Redis:Persistent:ConnectionString").Value;
                return ConnectionMultiplexer.Connect(connectionString!);
            });

            _inMemoryRedis = new Lazy<IConnectionMultiplexer>(() =>
            {
                var connectionString = _configuration.GetSection("Redis:InMemory:ConnectionString").Value;
                return ConnectionMultiplexer.Connect(connectionString!);
            });
        }

        public IConnectionMultiplexer GetConnection(bool usePersistent)
        {
            return usePersistent ? _persistentRedis.Value : _inMemoryRedis.Value;
        }
    }
}
