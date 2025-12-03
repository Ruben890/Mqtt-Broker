using Application.Contract.IRedis;
using Infrastructure.Interfaces;
using StackExchange.Redis;

namespace Infrastructure.Redis
{
    public class RedisClientMapping : IRedisClientMapping
    {
        private readonly IDatabase _db;

        public RedisClientMapping(IRedisService redisService)
        {
            // Decide si usar persistencia según tu lógica
            bool usarAOF = true; // o false según tu lógica
            var redisConnection = redisService.GetConnection(usarAOF);

            // Obtenemos la base de datos
            _db = redisConnection.GetDatabase();
        }

        public async Task SetClientAsync(string chipId, string clientId, TimeSpan? expiry = null)
        {
            await _db.StringSetAsync($"mqtt:client:{chipId}", clientId, expiry);
        }

        public async Task<bool> TryAddClientAsync(string chipId, string clientId, TimeSpan? expiry = null)
        {
            return await _db.StringSetAsync($"mqtt:client:{chipId}", clientId, expiry, when: When.NotExists);
        }

        public async Task SetOrUpdateClientAsync(string chipId, string clientId, TimeSpan? expiry = null)
        {
            await _db.StringSetAsync($"mqtt:client:{chipId}", clientId, expiry);
        }

        public async Task<string?> GetClientAsync(string chipId)
        {
            return await _db.StringGetAsync($"mqtt:client:{chipId}");
        }

        public async Task RemoveClientAsync(string chipId)
        {
            await _db.KeyDeleteAsync($"mqtt:client:{chipId}");
        }

        public async Task<string?> GetChipIdByClientIdAsync(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                return null;

            // Obtenemos todas las claves que coincidan con clientes MQTT
            var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: "mqtt:client:*");

            foreach (var key in keys)
            {
                var value = await _db.StringGetAsync(key);
                if (value == clientId)
                {
                    // Extraemos el ChipId del key
                    return key.ToString().Replace("mqtt:client:", "");
                }
            }

            return null;
        }
    }
}
