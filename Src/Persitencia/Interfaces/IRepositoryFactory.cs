using Microsoft.EntityFrameworkCore;

namespace Persitencia.Interfaces
{
    public interface IRepositoryFactory
    {
        TRepository CreateRepository<TRepository>(DbContext context) where TRepository : class;
    }
}
