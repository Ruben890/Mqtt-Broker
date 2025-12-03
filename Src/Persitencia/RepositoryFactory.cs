using Application.Contract.IRepositories;
using Microsoft.EntityFrameworkCore;
using Persitencia.Contexts;
using Persitencia.Interfaces;
using Persitencia.Repositories;
using System.Collections.Concurrent;

namespace Persitencia
{
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly Dictionary<Type, Func<DbContext, object>> _factories;
        private readonly ConcurrentDictionary<Tuple<Type, DbContext>, Lazy<object>> _repositoryCache;

        public RepositoryFactory()
        {
            _repositoryCache = new ConcurrentDictionary<Tuple<Type, DbContext>, Lazy<object>>();
            _factories = new Dictionary<Type, Func<DbContext, object>>
            {
                { typeof(IDeviceRepository), ctx => new DeviceRepository((MqttBrokerContext)ctx) },
                { typeof(ITelemetryRecordRepository), ctx => new TelemetryRecordRepository((MqttBrokerContext)ctx) },
                { typeof(IFirmwareVersionRecordRepository), ctx => new FirmwareVersionRecordRepository((MqttBrokerContext)ctx) },
                { typeof(IDeviceGroupRepository), ctx => new DeviceGroupRepository((MqttBrokerContext)ctx) },
            };
        }

        public TRepository CreateRepository<TRepository>(DbContext context) where TRepository : class
        {
            var repositoryInterfaceType = typeof(TRepository);

            if (!_factories.TryGetValue(repositoryInterfaceType, out var factory))
            {
                throw new ArgumentException($"No repository factory found for {repositoryInterfaceType.Name}");
            }

            var cacheKey = Tuple.Create(repositoryInterfaceType, context);

            var lazyRepository = _repositoryCache.GetOrAdd(cacheKey,
                _ => new Lazy<object>(() => factory(context)));

            return lazyRepository.Value as TRepository
                ?? throw new InvalidCastException($"Cannot cast repository for {repositoryInterfaceType.Name} to {typeof(TRepository).Name}");
        }

    }
}
