using Application.Contract.IRepositories;
using Application.Contract.IUnitOfWork;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore.Storage;
using Persitencia.Contexts;
using Persitencia.Interfaces;
using System.Collections.Concurrent;

namespace Persitencia.UnitOfWork
{
    public class MqttBrokerUnitOfWorkManager : IMqttBrokerUnitOfWorkManager, IDisposable, IAsyncDisposable
    {
        private readonly MqttBrokerContext _context;
        private readonly IRepositoryFactory _repositoryFactory;
        private readonly ConcurrentDictionary<Type, object> _repositories = new();
        private IDbContextTransaction? _currentTransaction;
        public MqttBrokerUnitOfWorkManager(MqttBrokerContext context, IRepositoryFactory repositoryFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
        }



        public IDeviceRepository DeviceRepository => GetRepository<IDeviceRepository>();

        public ITelemetryRecordRepository TelemetryRecordRepository => GetRepository<ITelemetryRecordRepository>();

        public IFirmwareVersionRecordRepository FirmwareVersionRecordRepository => GetRepository<IFirmwareVersionRecordRepository>();
        public IDeviceGroupRepository DeviceGroupRepository => GetRepository<IDeviceGroupRepository>();

        public bool HasActiveTransaction => _currentTransaction != null;

        public async Task BulkSaveChangesAsync() => await _context.BulkSaveChangesAsync();

        public async Task BeginAsync()
        {
            if (_currentTransaction == null)
                _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task SaveAsync() => await _context.SaveChangesAsync();

       
        public IMqttBrokerUnitOfWorkManager CreateNewScope()
        {
            // Obtener las opciones de DbContext actuales
            var options = _context.GetDbContextOptions();

            // Crear un nuevo contexto independiente
            var newContext = new MqttBrokerContext(options);

            // Retornar un nuevo UnitOfWork con todos los repositorios
            return new MqttBrokerUnitOfWorkManager(newContext, _repositoryFactory);
        }

        public void Dispose()
        {
            // Dispose del DbContext y de la transacción si existen
            _currentTransaction?.Dispose();
            _context.Dispose();
        }

        // Si quieres async disposal
        public async ValueTask DisposeAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
            }
            await _context.DisposeAsync();
        }

        private TRepository GetRepository<TRepository>() where TRepository : class
        {
            var lazy = (Lazy<TRepository>)_repositories.GetOrAdd(
                typeof(TRepository),
                _ => new Lazy<TRepository>(() => _repositoryFactory.CreateRepository<TRepository>(_context))
            );

            return lazy.Value;
        }
    }
}
