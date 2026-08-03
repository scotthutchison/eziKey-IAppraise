using IAppraise.Contracts;
using Integrations;
using Integrations.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IAppraise.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingsController : ControllerBase
    {
        // Headers the touchscreen must send on every call so this API can route to the
        // correct TDL dealership with the correct credentials.
        private const string DealershipHeader = "X-Tdl-Dealership-Id";
        private const string TokenHeader = "X-Tdl-Api-Token";

        private readonly IIAppraiseApi _iAppraiseApi;

        public BookingsController(IIAppraiseApi iAppraiseApi)
        {
            _iAppraiseApi = iAppraiseApi;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingSummaryDto>>> GetOpenBookings()
        {
            if (!TryGetContext(out var ctx, out var badRequest)) return badRequest!;

            var result = await _iAppraiseApi.GetAllUnstartedVehicleEvents(ctx);
            if (!result.Succeeded)
                return Problem(string.Join("; ", result.ErrorList), statusCode: 502);

            // Trust the ezikey-get-all-unstarted-vehicle-events endpoint to have already filtered
            // to pending events. An earlier attempt filtered client-side on DateTimeStarted == null,
            // but that field is set by TDL when the customer creates/schedules the booking (not when
            // the drive is started by eziKey), so filtering on it silently dropped every real
            // pending booking whose customer had entered a schedule time.
            var events = result.Value?.VehicleEvents ?? new();
            var bookings = events.Select(e => new BookingSummaryDto
            {
                BookingId = e.Id,
                CustomerFirstName = e.Customer?.FirstName,
                CustomerLastName = e.Customer?.LastName,
                CustomerPhoneNumber = e.Customer?.PhoneNumber,
                CustomerEmail = e.Customer?.Email,
                CustomerSuburb = e.Customer?.Suburb,
                CustomerTitle = e.Customer?.Title,
                DealerFirstName = e.Dealer?.FirstName,
                DealerLastName = e.Dealer?.LastName,
                DateTimeStarted = e.DateTimeStarted,
            });

            return Ok(bookings);
        }

        [HttpPost("{bookingId:int}/pickup")]
        public async Task<ActionResult<PickupResponseDto>> Pickup(int bookingId, [FromBody] PickupRequestDto request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            if (!TryGetContext(out var ctx, out var badRequest)) return badRequest!;

            // WPF doesn't know TDL's internal vehicle id — it only knows rego / stock. Resolve here
            // by pulling the dealership's vehicle list from TDL and matching. If no match and
            // request.CreateIfMissing is true, POST the vehicle to TDL first, then use the new id.
            var tdlVehicleId = request.IAppraiseVehicleId;
            var vehicleWasCreated = false;

            if (!tdlVehicleId.HasValue)
            {
                var vehiclesResult = await _iAppraiseApi.GetAllVehicles(ctx);
                if (!vehiclesResult.Succeeded)
                    return Problem("Could not fetch TDL vehicle list: " + string.Join("; ", vehiclesResult.ErrorList ?? new() { "unknown" }), statusCode: 502);

                var vehicles = vehiclesResult.Value?.Vehicles ?? new List<VehicleDto>();
                VehicleDto? match = null;

                if (!string.IsNullOrWhiteSpace(request.RegistrationNumber))
                    match = vehicles.FirstOrDefault(v => string.Equals((v.RegistrationNumber ?? "").Trim(), request.RegistrationNumber.Trim(), StringComparison.OrdinalIgnoreCase));

                if (match == null && !string.IsNullOrWhiteSpace(request.StockNumber))
                    match = vehicles.FirstOrDefault(v => string.Equals((v.StockNumber ?? "").Trim(), request.StockNumber.Trim(), StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    tdlVehicleId = match.Id;
                }
                else if (request.CreateIfMissing)
                {
                    var createResult = await _iAppraiseApi.CreateVehicle(ctx, new CreateVehicleRequestDto
                    {
                        Dealership = ctx.DealershipId,
                        Make = request.Make,
                        Model = request.Model,
                        ModelYear = request.ModelYear,
                        NewUsed = request.NewUsed,
                        StockNumber = request.StockNumber,
                        RegistrationNumber = request.RegistrationNumber,
                        VinNumber = request.VinNumber,
                        Colour = request.Colour,
                        Odometer = request.Odometer,
                    });

                    if (!createResult.Succeeded || createResult.Value == null)
                        return Problem($"TDL vehicle create failed for rego='{request.RegistrationNumber}' stock='{request.StockNumber}': {string.Join("; ", createResult.ErrorList ?? new() { "unknown" })}", statusCode: 502);

                    tdlVehicleId = createResult.Value.Id;
                    vehicleWasCreated = true;
                }
                else
                {
                    var sample = string.Join(", ", vehicles.Take(5).Select(v => $"[{v.Id}] rego='{v.RegistrationNumber}' stock='{v.StockNumber}'"));
                    if (vehicles.Count > 5) sample += $" (+{vehicles.Count - 5} more)";
                    if (vehicles.Count == 0) sample = "(TDL returned no vehicles for this dealership)";

                    return NotFound($"No TDL vehicle found matching rego='{request.RegistrationNumber}' or stock='{request.StockNumber}' and createIfMissing was false. TDL returned {vehicles.Count} vehicle(s): {sample}");
                }
            }

            var result = await _iAppraiseApi.StartDrive(ctx, bookingId, tdlVehicleId.Value);
            if (!result.Succeeded || result.Value == null)
                return Problem(string.Join("; ", result.ErrorList ?? new() { "StartDrive failed" }), statusCode: 502);

            return Ok(new PickupResponseDto
            {
                BookingId = result.Value.Id,
                IAppraiseVehicleId = result.Value.Vehicle?.Id ?? tdlVehicleId.Value,
                VehicleCreated = vehicleWasCreated,
            });
        }

        [HttpPost("{bookingId:int}/return")]
        public async Task<ActionResult<ReturnResponseDto>> Return(int bookingId, [FromBody] ReturnRequestDto request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            if (!TryGetContext(out var ctx, out var badRequest)) return badRequest!;

            var result = await _iAppraiseApi.EndDrive(ctx, bookingId, request.ReturningOdometer, request.ReturningFuelLevel ?? string.Empty);
            if (!result.Succeeded || result.Value == null)
                return Problem(string.Join("; ", result.ErrorList ?? new() { "EndDrive failed" }), statusCode: 502);

            return Ok(new ReturnResponseDto { BookingId = result.Value.Id });
        }

        private bool TryGetContext(out TdlContext ctx, out ActionResult? badRequest)
        {
            ctx = null!;
            badRequest = null;

            var dealershipRaw = Request.Headers[DealershipHeader].ToString();
            var token = Request.Headers[TokenHeader].ToString();

            if (string.IsNullOrWhiteSpace(dealershipRaw))
            {
                badRequest = BadRequest($"Missing required header '{DealershipHeader}'.");
                return false;
            }
            if (!int.TryParse(dealershipRaw, out var dealershipId))
            {
                badRequest = BadRequest($"Header '{DealershipHeader}' must be an integer; got '{dealershipRaw}'.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(token))
            {
                badRequest = BadRequest($"Missing required header '{TokenHeader}'.");
                return false;
            }

            ctx = new TdlContext(dealershipId, token);
            return true;
        }
    }
}
