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
        private readonly IIAppraiseApi _iAppraiseApi;

        public BookingsController(IIAppraiseApi iAppraiseApi)
        {
            _iAppraiseApi = iAppraiseApi;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingSummaryDto>>> GetOpenBookings()
        {
            var result = await _iAppraiseApi.GetAllUnstartedVehicleEvents();
            if (!result.Succeeded)
                return Problem(string.Join("; ", result.ErrorList), statusCode: 502);

            // TDL's ezikey-get-all-unstarted-vehicle-events endpoint should return only pending
            // bookings but has been observed returning commenced/completed drives as well.
            // Filter defensively — an event with DateTimeStarted set is no longer pending.
            var events = result.Value?.VehicleEvents ?? new();
            var bookings = events
                .Where(e => e.DateTimeStarted == null)
                .Select(e => new BookingSummaryDto
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

            // WPF doesn't know TDL's internal vehicle id — it only knows rego / stock. Resolve here
            // by pulling the site's vehicle list from TDL and matching. createIfMissing is still a
            // TODO (needs a TDL create-vehicle endpoint).
            var tdlVehicleId = request.IAppraiseVehicleId;

            if (!tdlVehicleId.HasValue)
            {
                var vehiclesResult = await _iAppraiseApi.GetAllVehicles();
                if (!vehiclesResult.Succeeded)
                    return Problem("Could not fetch TDL vehicle list: " + string.Join("; ", vehiclesResult.ErrorList ?? new() { "unknown" }), statusCode: 502);

                var vehicles = vehiclesResult.Value?.Vehicles ?? new List<VehicleDto>();
                VehicleDto? match = null;

                if (!string.IsNullOrWhiteSpace(request.RegistrationNumber))
                    match = vehicles.FirstOrDefault(v => string.Equals(v.RegistrationNumber, request.RegistrationNumber, StringComparison.OrdinalIgnoreCase));

                if (match == null && !string.IsNullOrWhiteSpace(request.StockNumber))
                    match = vehicles.FirstOrDefault(v => string.Equals(v.StockNumber, request.StockNumber, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                    return NotFound($"No TDL vehicle found matching rego='{request.RegistrationNumber}' or stock='{request.StockNumber}'. createIfMissing is not yet supported by the local facade.");

                tdlVehicleId = match.Id;
            }

            var result = await _iAppraiseApi.StartDrive(bookingId, tdlVehicleId.Value);
            if (!result.Succeeded || result.Value == null)
                return Problem(string.Join("; ", result.ErrorList ?? new() { "StartDrive failed" }), statusCode: 502);

            return Ok(new PickupResponseDto
            {
                BookingId = result.Value.Id,
                IAppraiseVehicleId = result.Value.Vehicle?.Id ?? tdlVehicleId.Value,
                VehicleCreated = false,
            });
        }

        [HttpPost("{bookingId:int}/return")]
        public async Task<ActionResult<ReturnResponseDto>> Return(int bookingId, [FromBody] ReturnRequestDto request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var result = await _iAppraiseApi.EndDrive(bookingId, request.ReturningOdometer, request.ReturningFuelLevel ?? string.Empty);
            if (!result.Succeeded || result.Value == null)
                return Problem(string.Join("; ", result.ErrorList ?? new() { "EndDrive failed" }), statusCode: 502);

            return Ok(new ReturnResponseDto { BookingId = result.Value.Id });
        }
    }
}
