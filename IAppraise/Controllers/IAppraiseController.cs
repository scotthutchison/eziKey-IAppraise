using Integrations;
using Integrations.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IAppraise.Controllers
{
    /// <summary>
    /// Diagnostic / manual endpoints. The touchscreen uses <see cref="BookingsController"/>;
    /// these are for debugging or ad-hoc calls. Same tenant headers required.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class IAppraiseController : ControllerBase
    {
        private const string DealershipHeader = "X-Tdl-Dealership-Id";
        private const string TokenHeader = "X-Tdl-Api-Token";
        private const string ApiKeyHeader = "X-Api-Key";

        private readonly IIAppraiseApi _iAppraiseApi;
        private readonly IConfiguration _config;

        public IAppraiseController(IIAppraiseApi iAppraiseApi, IConfiguration config)
        {
            _iAppraiseApi = iAppraiseApi;
            _config = config;
        }

        [HttpGet("vehicles")]
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAllVehicles()
        {
            if (!TryGetContext(out var ctx, out var bad)) return bad!;
            var result = await _iAppraiseApi.GetAllVehicles(ctx);
            return Ok(result.Value?.Vehicles ?? Enumerable.Empty<VehicleDto>());
        }

        [HttpGet("vehicle-events")]
        public async Task<ActionResult<IEnumerable<VehicleEventDto>>> GetAllUnstartedVehicleEvents()
        {
            if (!TryGetContext(out var ctx, out var bad)) return bad!;
            var result = await _iAppraiseApi.GetAllUnstartedVehicleEvents(ctx);
            return Ok(result.Value?.VehicleEvents ?? Enumerable.Empty<VehicleEventDto>());
        }

        [HttpPost("StartDrive")]
        public async Task<ActionResult<VehicleDriveDto>> StartDrive(int driveId, int vehicleId)
        {
            if (!TryGetContext(out var ctx, out var bad)) return bad!;
            var result = await _iAppraiseApi.StartDrive(ctx, driveId, vehicleId);
            return Ok(result.Value);
        }

        /// <summary>
        /// Diagnostic: POST a vehicle straight to TDL, isolated from the pickup flow. Useful to
        /// verify the token/dealership combination can actually create vehicles in TDL. Returns
        /// the raw TDL response (or the TDL error body) so failures are diagnosable end-to-end.
        /// </summary>
        [HttpPost("create-vehicle")]
        public async Task<ActionResult<object>> CreateVehicle([FromBody] CreateVehicleRequestDto request)
        {
            if (!TryGetContext(out var ctx, out var bad)) return bad!;
            if (request == null) return BadRequest("Request body is required.");

            var result = await _iAppraiseApi.CreateVehicle(ctx, request);
            if (!result.Succeeded || result.Value == null)
                return Problem(string.Join("; ", result.ErrorList ?? new() { "unknown" }), statusCode: 502);

            return Ok(new
            {
                created = true,
                tdlVehicleId = result.Value.Id,
                registrationNumber = result.Value.RegistrationNumber,
                stockNumber = result.Value.StockNumber,
                make = result.Value.Make,
                model = result.Value.Model,
                modelYear = result.Value.ModelYear,
                colour = result.Value.Colour,
                odometer = result.Value.Odometer,
                newUsed = result.Value.NewUsed,
                isActive = result.Value.IsActive,
                isManualEntry = result.Value.IsManualEntry,
            });
        }

        [HttpPost("EndDrive")]
        public async Task<ActionResult<VehicleDriveDto>> EndDrive(int driveId, int? returningOdometer, string returningFuelLevel)
        {
            if (!TryGetContext(out var ctx, out var bad)) return bad!;
            var result = await _iAppraiseApi.EndDrive(ctx, driveId, returningOdometer, returningFuelLevel);
            return Ok(result.Value);
        }

        private bool TryGetContext(out TdlContext ctx, out ActionResult? badRequest)
        {
            ctx = null!;
            badRequest = null;

            var dealershipRaw = Request.Headers[DealershipHeader].ToString();
            var token = Request.Headers[TokenHeader].ToString();

            if (!string.IsNullOrWhiteSpace(dealershipRaw) && !string.IsNullOrWhiteSpace(token))
            {
                if (!int.TryParse(dealershipRaw, out var dealershipId))
                {
                    badRequest = BadRequest($"Header '{DealershipHeader}' must be an integer; got '{dealershipRaw}'.");
                    return false;
                }
                ctx = new TdlContext(dealershipId, token);
                return true;
            }

            var apiKey = Request.Headers[ApiKeyHeader].ToString();
            var configuredKey = _config["IAppraise:ApiKey"] ?? "";
            if (!string.IsNullOrWhiteSpace(apiKey) && apiKey == configuredKey)
            {
                var defaultDealership = int.Parse(_config["IAppraise:DefaultDealershipId"] ?? "0");
                var defaultToken = _config["IAppraise:DefaultTdlToken"] ?? "";
                if (defaultDealership > 0 && !string.IsNullOrWhiteSpace(defaultToken))
                {
                    ctx = new TdlContext(defaultDealership, defaultToken);
                    return true;
                }
            }

            badRequest = Unauthorized("Provide X-Tdl-Dealership-Id + X-Tdl-Api-Token headers, or a valid X-Api-Key.");
            return false;
        }
    }
}
