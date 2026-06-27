using Integrations;
using Integrations.Configuration;
using Integrations.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IAppraise.Controllers
{
    [ApiController]
    [Route("api/iappraise")]
    public class IAppraiseController : ControllerBase
    {
        private readonly IIAppraiseApi _iAppraiseApi;
        private readonly IAppraiseOptions _options;

        public IAppraiseController(IIAppraiseApi iAppraiseApi, IOptions<IAppraiseOptions> options)
        {
            _iAppraiseApi = iAppraiseApi;
            _options = options.Value;
        }

        [HttpGet("vehicles")]
        public async Task<IEnumerable<VehicleDto>> GetAllVehicles(
            [FromQuery] int? dealershipId,
            [FromQuery] string? registrationNumber,
            [FromQuery] string? stockNumber,
            CancellationToken ct)
        {
            var dealership = dealershipId ?? _options.DefaultDealershipId;
            var result = await _iAppraiseApi.GetAllVehicles(dealership, registrationNumber, stockNumber, ct);
            return result.Value?.Vehicles ?? Enumerable.Empty<VehicleDto>();
        }

        [HttpGet("vehicle-events/unstarted")]
        public async Task<IEnumerable<VehicleEventDto>> GetAllUnstartedVehicleEvents(
            [FromQuery] int? dealershipId,
            CancellationToken ct)
        {
            var dealership = dealershipId ?? _options.DefaultDealershipId;
            var result = await _iAppraiseApi.GetAllUnstartedVehicleEvents(dealership, ct);
            return result.Value?.VehicleEvents ?? Enumerable.Empty<VehicleEventDto>();
        }

        [HttpPost("vehicle-events/{driveId:int}/start")]
        public async Task<ActionResult<VehicleDriveDto>> StartDrive(
            int driveId,
            [FromQuery] int vehicleId,
            CancellationToken ct)
        {
            var result = await _iAppraiseApi.StartDrive(driveId, vehicleId, ct);
            if (!result.Succeeded)
            {
                return Problem(string.Join("; ", result.ErrorList), statusCode: StatusCodes.Status502BadGateway);
            }
            return Ok(result.Value);
        }

        [HttpPost("vehicle-events/{driveId:int}/end")]
        public async Task<ActionResult<VehicleDriveDto>> EndDrive(
            int driveId,
            [FromQuery] int returningOdometer,
            [FromQuery] string returningFuelLevel,
            CancellationToken ct)
        {
            var result = await _iAppraiseApi.EndDrive(driveId, returningOdometer, returningFuelLevel, ct);
            if (!result.Succeeded)
            {
                return Problem(string.Join("; ", result.ErrorList), statusCode: StatusCodes.Status502BadGateway);
            }
            return Ok(result.Value);
        }
    }
}
