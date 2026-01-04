using Integrations;
using Integrations.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IAppraise.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IAppraiseController : ControllerBase
    {
        private IIAppraiseApi _iAppraiseApi;

        public IAppraiseController(IIAppraiseApi iAppraiseApi)
        {
            _iAppraiseApi = iAppraiseApi;
        }

        [HttpGet(Name = "GetAllVehicles")]
        public async Task<IEnumerable<VehicleDto>> GetAllVehicles()
        {
            var result = await _iAppraiseApi.GetAllVehicles();
            return result.Value?.Vehicles ?? Enumerable.Empty<VehicleDto>();
        }

        [HttpGet(Name = "GetAllUnstartedVehicleEvents")]
        public async Task<IEnumerable<VehicleEventDto>> GetAllUnstartedVehicleEvents()
        {
            var result = await _iAppraiseApi.GetAllUnstartedVehicleEvents();
            return result.Value?.VehicleEvents ?? Enumerable.Empty<VehicleEventDto>();
        }

        [HttpPost("StartDrive")]
            public async Task<VehicleDriveDto> StartDrive(int driveId, int vehicleId)
        {
            var result = await _iAppraiseApi.StartDrive(driveId, vehicleId);
            return result.Value;
        }

        [HttpPost("EndDrive")]
        public async Task<VehicleDriveDto> EndDrive(int driveId, int returningOdometer, string returningFuelLevel)
        {
            var result = await _iAppraiseApi.EndDrive(driveId, returningOdometer, returningFuelLevel);
            return result.Value;
        }
    }
}
