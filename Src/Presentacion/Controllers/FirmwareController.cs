using Application.Contract.IServcies;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;
using Shared.Request;

namespace Presentacion.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/MqttBroker/[controller]")]
    [ApiController]
    public class FirmwareController : ControllerBase
    {
        private readonly IFirmwareServices _firmwareServices;

        public FirmwareController(IFirmwareServices firmwareServices)
        {
            _firmwareServices = firmwareServices;
        }



        [HttpGet("GetFirmwareVersionRecords")]
        public async Task<IActionResult> GetFirmwareVersionRecords([FromQuery] GenericParameters parameters) =>
            new ObjectResult( await _firmwareServices.GetFirmwareVersionRecords(parameters));

        [HttpPost("UpdateFirmwareVersion")]
        public async Task<IActionResult> UpdateFirmwareVersion([FromQuery] GenericParameters parameters, [FromForm] UpdateFirmwareDto request) =>
                new ObjectResult(await _firmwareServices.UpdateFirmwareVersion(parameters, request));


        [HttpGet("RollbackFirmwareVersion")]
        public async Task<IActionResult> RollbackFirmwareVersion([FromQuery] GenericParameters parameters) =>
                new ObjectResult(await _firmwareServices.RollbackFirmwareVersion(parameters));

    }
}
