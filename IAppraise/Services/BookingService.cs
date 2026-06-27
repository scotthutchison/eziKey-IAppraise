using IAppraise.Models;
using Integrations;
using Integrations.Configuration;
using Integrations.Core;
using Integrations.Dtos;
using Microsoft.Extensions.Options;

namespace IAppraise.Services
{
    public interface IBookingService
    {
        Task<Result<List<BookingSummaryDto>>> GetOpenBookingsAsync(CancellationToken ct);
        Task<Result<PickupResponseDto>> PickupAsync(int bookingId, PickupRequestDto request, CancellationToken ct);
        Task<Result<ReturnResponseDto>> ReturnAsync(int bookingId, ReturnRequestDto request, CancellationToken ct);
    }

    public class BookingService : IBookingService
    {
        private readonly IIAppraiseApi _api;
        private readonly IAppraiseOptions _options;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            IIAppraiseApi api,
            IOptions<IAppraiseOptions> options,
            ILogger<BookingService> logger)
        {
            _api = api;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<Result<List<BookingSummaryDto>>> GetOpenBookingsAsync(CancellationToken ct)
        {
            var dealershipId = _options.DefaultDealershipId;
            _logger.LogInformation("Fetching open bookings for dealership {DealershipId}", dealershipId);

            var result = await _api.GetAllUnstartedVehicleEvents(dealershipId, ct);
            if (!result.Succeeded)
            {
                return new Result<List<BookingSummaryDto>>(result.ErrorList.ToArray());
            }

            var list = result.Value.VehicleEvents
                .Where(ev => !ev.DateTimeStarted.HasValue)
                .Select(ev => new BookingSummaryDto
                {
                    BookingId = ev.Id,
                    CustomerFirstName = ev.Customer?.FirstName,
                    CustomerLastName = ev.Customer?.LastName,
                    CustomerPhoneNumber = ev.Customer?.PhoneNumber,
                    CustomerEmail = ev.Customer?.Email,
                    CustomerSuburb = ev.Customer?.Suburb,
                    CustomerTitle = ev.Customer?.Title,
                    DealerFirstName = ev.Dealer?.FirstName,
                    DealerLastName = ev.Dealer?.LastName,
                    DateTimeStarted = ev.DateTimeStarted
                })
                .ToList();

            return new Result<List<BookingSummaryDto>>(list);
        }

        public async Task<Result<PickupResponseDto>> PickupAsync(int bookingId, PickupRequestDto request, CancellationToken ct)
        {
            var dealershipId = _options.DefaultDealershipId;

            var vehicleResult = await ResolveVehicleIdAsync(dealershipId, request, ct);
            if (!vehicleResult.Succeeded)
            {
                return new Result<PickupResponseDto>(vehicleResult.ErrorList.ToArray());
            }

            _logger.LogInformation(
                "Starting iAppraise drive {BookingId} with vehicle {VehicleId} (dealership {DealershipId})",
                bookingId, vehicleResult.Value.VehicleId, dealershipId);

            var drive = await _api.StartDrive(bookingId, vehicleResult.Value.VehicleId, ct);
            if (!drive.Succeeded)
            {
                return new Result<PickupResponseDto>(drive.ErrorList.ToArray());
            }

            return new Result<PickupResponseDto>(new PickupResponseDto
            {
                BookingId = drive.Value.Id,
                IAppraiseVehicleId = drive.Value.Vehicle?.Id ?? vehicleResult.Value.VehicleId,
                VehicleCreated = vehicleResult.Value.Created
            });
        }

        public async Task<Result<ReturnResponseDto>> ReturnAsync(int bookingId, ReturnRequestDto request, CancellationToken ct)
        {
            _logger.LogInformation(
                "Ending iAppraise drive {BookingId} with odometer {Odometer} fuel {Fuel}",
                bookingId, request.ReturningOdometer?.ToString() ?? "(none)", request.ReturningFuelLevel);

            var drive = await _api.EndDrive(bookingId, request.ReturningOdometer, request.ReturningFuelLevel, ct);
            if (!drive.Succeeded)
            {
                return new Result<ReturnResponseDto>(drive.ErrorList.ToArray());
            }

            return new Result<ReturnResponseDto>(new ReturnResponseDto
            {
                BookingId = drive.Value.Id
            });
        }

        private async Task<Result<VehicleResolution>> ResolveVehicleIdAsync(int dealershipId, PickupRequestDto request, CancellationToken ct)
        {
            if (request.IAppraiseVehicleId.HasValue && request.IAppraiseVehicleId.Value > 0)
            {
                return new Result<VehicleResolution>(new VehicleResolution(request.IAppraiseVehicleId.Value, false));
            }

            if (!string.IsNullOrWhiteSpace(request.RegistrationNumber))
            {
                var lookup = await _api.GetAllVehicles(dealershipId, registrationNumber: request.RegistrationNumber, ct: ct);
                if (!lookup.Succeeded)
                {
                    return new Result<VehicleResolution>(lookup.ErrorList.ToArray());
                }

                var match = lookup.Value.Vehicles.FirstOrDefault(v => v.IsActive);
                if (match is not null)
                {
                    return new Result<VehicleResolution>(new VehicleResolution(match.Id, false));
                }
            }

            if (!string.IsNullOrWhiteSpace(request.StockNumber))
            {
                var lookup = await _api.GetAllVehicles(dealershipId, stockNumber: request.StockNumber, ct: ct);
                if (!lookup.Succeeded)
                {
                    return new Result<VehicleResolution>(lookup.ErrorList.ToArray());
                }

                var match = lookup.Value.Vehicles.FirstOrDefault(v => v.IsActive);
                if (match is not null)
                {
                    return new Result<VehicleResolution>(new VehicleResolution(match.Id, false));
                }
            }

            if (!request.CreateIfMissing)
            {
                return new Result<VehicleResolution>(
                    "Vehicle not found in iAppraise by registration or stock number, and CreateIfMissing is false.");
            }

            if (string.IsNullOrWhiteSpace(request.RegistrationNumber)
                && string.IsNullOrWhiteSpace(request.StockNumber)
                && string.IsNullOrWhiteSpace(request.VinNumber))
            {
                return new Result<VehicleResolution>(
                    "Cannot create vehicle in iAppraise: registration, stock number, and VIN are all empty.");
            }

            _logger.LogInformation(
                "Vehicle {Rego}/{Stock} not found in iAppraise dealership {DealershipId}; creating.",
                request.RegistrationNumber, request.StockNumber, dealershipId);

            var created = await _api.CreateVehicle(new CreateVehicleRequestDto
            {
                Dealership = dealershipId,
                Make = request.Make,
                Model = request.Model,
                ModelYear = request.ModelYear,
                NewUsed = request.NewUsed,
                StockNumber = request.StockNumber,
                RegistrationNumber = request.RegistrationNumber,
                VinNumber = request.VinNumber,
                Colour = request.Colour,
                Odometer = request.Odometer
            }, ct);

            if (!created.Succeeded)
            {
                return new Result<VehicleResolution>(created.ErrorList.ToArray());
            }

            return new Result<VehicleResolution>(new VehicleResolution(created.Value.Id, true));
        }

        private record VehicleResolution(int VehicleId, bool Created);
    }
}
