using StackExchange.Redis;

namespace Infrastructure.Interfaces
{
    public interface IRedisService
    {
        IConnectionMultiplexer GetConnection(bool usePersistent);
    }
}
