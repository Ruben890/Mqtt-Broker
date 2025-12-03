using Domain.Entities;

namespace Application.Contract.IRepositories
{
    public interface IDeviceGroupRepository
    {
        Task CreateGroupAsync(Groups group);
        void DeleteGroup(Groups group);
        Task<IEnumerable<Groups>> GetAllGroupsByUserIdAsync(Guid userId);
        Task<Groups?> GetDeviceGroupByName(string groupName);
        Task<Groups?> GetGroupByCodeAsync(string code);
        void UpdateGroup(Groups group);
    }
}
