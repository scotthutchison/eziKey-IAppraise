using IAppraise.Contracts;
using Integrations;
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

            var events = result.Value?.VehicleEvents ?? new();
            var bookings = events.Where(e => !e.DateTimeStarted.HasValue).Select(e => new BookingSummaryDto
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

            if (!request.IAppraiseVehicleId.HasValue)
            {
                // TODO: support createIfMissing + vehicle lookup by rego/stock against testdriveloan.
                return BadRequest("iAppraiseVehicleId is required (createIfMissing is not yet supported by the local facade).");
            }

            var result = await _iAppraiseApi.StartDrive(bookingId, request.IAppraiseVehicleId.Value);
            if (!result.Succeeded || result.Value == null)
                return Problem(string.Join("; ", result.ErrorList ?? new() { "StartDrive failed" }), statusCode: 502);

            return Ok(new PickupResponseDto
            {
                BookingId = result.Value.Id,
                IAppraiseVehicleId = result.Value.Vehicle?.Id ?? request.IAppraiseVehicleId.Value,
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
