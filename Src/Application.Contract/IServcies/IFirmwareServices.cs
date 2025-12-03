using Shared.Dtos;
using Shared.Request;
using Shared.Response;

namespace Application.Contract.IServcies
{
    public interface IFirmwareServices
    {
        Task<BaseResponse> GetFirmwareVersionRecords(GenericParameters parameters);
        Task<BaseResponse> RollbackFirmwareVersion(GenericParameters parameters);
        Task<BaseResponse> UpdateFirmwareVersion(GenericParameters parameters, UpdateFirmwareDto request);
    }
}
