using Application.Contract.IRepositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persitencia.Contexts;

namespace Persitencia.Repositories
{
    public class DeviceGroupRepository : RepositoryBase<Groups, MqttBrokerContext>, IDeviceGroupRepository
    {
        private readonly MqttBrokerContext _context;
        public DeviceGroupRepository(MqttBrokerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Groups?> GetGroupByCodeAsync(string code) =>
         await FindByCondition(g => g.Code == code, false).FirstOrDefaultAsync();

        public async Task<IEnumerable<Groups>> GetAllGroupsByUserIdAsync(Guid userId) =>
         await FindByCondition(g => g.UserId == userId && g.IsActive, false).ToListAsync();


        public async Task<Groups?> GetDeviceGroupByName(string groupName) =>
         await FindByCondition(g => g.GroupName == groupName && g.IsActive, false).FirstOrDefaultAsync();

        public async Task CreateGroupAsync(Groups group) => await CreateAsyn(group);

        public void UpdateGroup(Groups group) => Update(group);

        public void DeleteGroup(Groups group) => Delete(group);


    }
}
