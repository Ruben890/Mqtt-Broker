using Shared.Request;
using Shared.Request.QueryParameters;
using Shared.Response;

namespace Application.Contract.IServcies
{
    public interface IDeviceServcies
    {
        Task<BaseResponse> GetDevices(DeviceParameters parameters);
    }
}
