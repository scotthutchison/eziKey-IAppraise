using Integrations.Core;
using Integrations.Dtos;
using Microsoft.Extensions.Configuration;
using System.Text.Json;


namespace Integrations
{
    public interface IIAppraiseApi
    {
        Task<Result<VehicleResponseDto>> GetAllVehicles(TdlContext ctx);
        Task<Result<VehicleEventsResponse>> GetAllUnstartedVehicleEvents(TdlContext ctx);
        Task<Result<VehicleDto>> CreateVehicle(TdlContext ctx, CreateVehicleRequestDto request);
        Task<Result<VehicleDriveDto>> StartDrive(TdlContext ctx, int driveId, int vehicleId);
        Task<Result<VehicleDriveDto>> EndDrive(TdlContext ctx, int driveId, int returningOdometer, string returningFuelLevel);
    }

    /// <summary>
    /// Per-request TDL tenant context. Passed in by the caller (touchscreen) so this API can
    /// serve multiple dealerships with a single deployment.
    /// </summary>
    public sealed record TdlContext(int DealershipId, string Token);

    public class IAppraiseApi : IIAppraiseApi
    {
        private readonly string _baseUrl;

        public IAppraiseApi(IConfiguration config)
        {
            // Only the TDL base URL is server-side config now — the dealership id and API token
            // come from the caller so this API can be shared across dealerships.
            _baseUrl = (config["IAppraise:BaseUrl"] ?? "https://www.testdriveloan.com.au").TrimEnd('/');
        }

        private HttpClient CreateClient(TdlContext ctx)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", ctx.Token);
            return client;
        }

        public async Task<Result<VehicleResponseDto>> GetAllVehicles(TdlContext ctx)
        {
            var client = CreateClient(ctx);
            var url = $"{_baseUrl}/api/vehicle/ezikey-list-all-vehicles-at-site/?dealership={ctx.DealershipId}";

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return new Result<VehicleResponseDto>(JsonSerializer.Deserialize<VehicleResponseDto>(json)!);
            }

            var body = await response.Content.ReadAsStringAsync();
            return new Result<VehicleResponseDto>($"TDL GetAllVehicles (dealership={ctx.DealershipId}) returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        }

        public async Task<Result<VehicleEventsResponse>> GetAllUnstartedVehicleEvents(TdlContext ctx)
        {
            var client = CreateClient(ctx);
            var url = $"{_baseUrl}/api/vehicle-event/ezikey-get-all-unstarted-vehicle-events/?dealership={ctx.DealershipId}";

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return new Result<VehicleEventsResponse>(JsonSerializer.Deserialize<VehicleEventsResponse>(json)!);
            }

            var body = await response.Content.ReadAsStringAsync();
            return new Result<VehicleEventsResponse>($"TDL GetAllUnstartedVehicleEvents (dealership={ctx.DealershipId}) returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        }

        public async Task<Result<VehicleDto>> CreateVehicle(TdlContext ctx, CreateVehicleRequestDto request)
        {
            // Force the dealership on the request to match the caller's context — the API is
            // multi-tenant and mustn't let one dealer create vehicles under another.
            request.Dealership = ctx.DealershipId;

            var client = CreateClient(ctx);
            var url = $"{_baseUrl}/api/vehicle/create-ezikey-vehicle/";

            var body = JsonSerializer.Serialize(request);
            var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync();
                return new Result<VehicleDto>($"TDL CreateVehicle (dealership={ctx.DealershipId}, rego='{request.RegistrationNumber}', stock='{request.StockNumber}') returned {(int)response.StatusCode} {response.ReasonPhrase}: {errBody}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<VehicleDto>(json);
            return new Result<VehicleDto>(dto!);
        }

        public async Task<Result<VehicleDriveDto>> StartDrive(TdlContext ctx, int driveId, int vehicleId)
        {
            var client = CreateClient(ctx);
            var url = $"{_baseUrl}/api/vehicle-event/{driveId}/ezikey-start-drive/";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(vehicleId.ToString()), "vehicle");

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            var response = await client.SendAsync(req);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return new Result<VehicleDriveDto>($"TDL StartDrive({driveId}, vehicle={vehicleId}) returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<VehicleDriveDto>(json);
            return new Result<VehicleDriveDto>(dto!);
        }

        public async Task<Result<VehicleDriveDto>> EndDrive(TdlContext ctx, int driveId, int returningOdometer, string returningFuelLevel)
        {
            var client = CreateClient(ctx);
            var url = $"{_baseUrl}/api/vehicle-event/{driveId}/ezikey-end-drive/";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(returningOdometer.ToString()), "returning_odometer");
            form.Add(new StringContent(returningFuelLevel), "returning_fuel_level");

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            var response = await client.SendAsync(req);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return new Result<VehicleDriveDto>($"TDL EndDrive({driveId}) returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<VehicleDriveDto>(json);
            return new Result<VehicleDriveDto>(dto!);
        }
    }
}
