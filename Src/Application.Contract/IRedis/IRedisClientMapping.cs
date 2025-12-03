namespace Application.Contract.IRedis
{
    public interface IRedisClientMapping
    {
        Task<string?> GetChipIdByClientIdAsync(string clientId);
        Task<string?> GetClientAsync(string chipId);
        Task RemoveClientAsync(string chipId);
        Task SetClientAsync(string chipId, string clientId, TimeSpan? expiry = null);
        Task SetOrUpdateClientAsync(string chipId, string clientId, TimeSpan? expiry = null);
        Task<bool> TryAddClientAsync(string chipId, string clientId, TimeSpan? expiry = null);
    }
}
