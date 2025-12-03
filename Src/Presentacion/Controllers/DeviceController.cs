using Application.Contract.IServcies;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Shared.Request.QueryParameters;

namespace Presentacion.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/MqttBroker/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceServcies _deviceServcies;

        public DeviceController(IDeviceServcies deviceServcies)
        {
            _deviceServcies = deviceServcies;
        }


        [HttpGet("GetDevices")]
        public async Task<IActionResult> GetDevices([FromQuery] DeviceParameters parameters) =>
            new ObjectResult(await _deviceServcies.GetDevices(parameters));

    }
}
