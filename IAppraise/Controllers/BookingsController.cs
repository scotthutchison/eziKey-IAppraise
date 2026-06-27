using IAppraise.Models;
using IAppraise.Services;
using Microsoft.AspNetCore.Mvc;

namespace IAppraise.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookings;

        public BookingsController(IBookingService bookings)
        {
            _bookings = bookings;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingSummaryDto>>> GetOpenBookings(
            CancellationToken ct)
        {
            var result = await _bookings.GetOpenBookingsAsync(ct);
            if (!result.Succeeded)
            {
                return Problem(string.Join("; ", result.ErrorList), statusCode: StatusCodes.Status502BadGateway);
            }
            return Ok(result.Value);
        }

        [HttpPost("{bookingId:int}/pickup")]
        public async Task<ActionResult<PickupResponseDto>> Pickup(
            int bookingId,
            [FromBody] PickupRequestDto request,
            CancellationToken ct)
        {
            var result = await _bookings.PickupAsync(bookingId, request, ct);
            if (!result.Succeeded)
            {
                return Problem(string.Join("; ", result.ErrorList), statusCode: StatusCodes.Status502BadGateway);
            }
            return Ok(result.Value);
        }

        [HttpPost("{bookingId:int}/return")]
        public async Task<ActionResult<ReturnResponseDto>> Return(
            int bookingId,
            [FromBody] ReturnRequestDto request,
            CancellationToken ct)
        {
            var result = await _bookings.ReturnAsync(bookingId, request, ct);
            if (!result.Succeeded)
            {
                return Problem(string.Join("; ", result.ErrorList), statusCode: StatusCodes.Status502BadGateway);
            }
            return Ok(result.Value);
        }
    }
}
