using System.Diagnostics;
using Integrations.Core;
using Integrations.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        private const int MaxLoggedBodyChars = 8 * 1024;

        private readonly string _baseUrl;
        private readonly ILogger<IAppraiseApi> _log;

        public IAppraiseApi(IConfiguration config, ILogger<IAppraiseApi>? log = null)
        {
            // Only the TDL base URL is server-side config now — the dealership id and API token
            // come from the caller so this API can be shared across dealerships.
            _baseUrl = (config["IAppraise:BaseUrl"] ?? "https://www.testdriveloan.com.au").TrimEnd('/');
            _log = log ?? NullLogger<IAppraiseApi>.Instance;
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

            var sw = Stopwatch.StartNew();
            _log.LogInformation("OUT GET {Url} (dealership={Dealership})", url, ctx.DealershipId);

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
            var body = await response.Content.ReadAsStringAsync();
            sw.Stop();
            LogResponse("GetAllVehicles", response, body, sw.ElapsedMilliseconds);

            if (response.IsSuccessStatusCode)
                return new Result<VehicleResponseDto>(JsonSerializer.Deserialize<VehicleResponseDto>(body)!);

            return new Result<VehicleResponseDto>($"TDL GetAllVehicles (dealership={ctx.DealershipId}) returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        }

        public async Task<Result<VehicleEventsResponse>> GetAllUnstartedVehicleEvents(TdlContext ctx)
        {
            var client = CreateClient(ctx);
            var url = $"{_baseUrl}/api/vehicle-event/ezikey-get-all-unstarted-vehicle-events/?dealership={ctx.DealershipId}";

            var sw = Stopwatch.StartNew();
            _log.LogInformation("OUT GET {Url} (dealership={Dealership})", url, ctx.DealershipId);

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
            var body = await response.Content.ReadAsStringAsync();
            sw.Stop();
            LogResponse("GetAllUnstartedVehicleEvents", response, body, sw.ElapsedMilliseconds);

            if (response.IsSuccessStatusCode)
                return new Result<VehicleEventsResponse>(JsonSerializer.Deserialize<VehicleEventsResponse>(body)!);

            return new Result<VehicleEventsResponse>($"TDL GetAllUnstartedVehicleEvents (dealership={ctx.DealershipId}) returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        }

        public async Task<Result<VehicleDto>> CreateVehicle(TdlContext ctx, CreateVehicleRequestDto request)
        {
            // Force the dealership on the request to match the caller's context — the API is
            // multi-tenant and mustn't let one dealer create vehicles under another.
            request.Dealership = ctx.DealershipId;

            var client = CreateClient(ctx);
            var url = $"{_baseUrl}/api/vehicle/create-ezikey-vehicle/";

            // TDL docs say Content-Type: application/json but the endpoint rejects JSON payloads
            // with "dealership: This field is required" even when dealership is clearly present.
            // Their other ezikey POST endpoints (StartDrive, EndDrive) accept multipart/form-data,
            // so use that here too.
            using var form = new MultipartFormDataContent();
            void AddPart(string name, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value)) form.Add(new StringContent(value), name);
            }
            AddPart("dealership", request.Dealership.ToString());
            AddPart("make", request.Make);
            AddPart("model", request.Model);
            if (request.ModelYear.HasValue) AddPart("model_year", request.ModelYear.Value.ToString());
            AddPart("new_used", request.NewUsed);
            AddPart("stock_number", request.StockNumber);
            AddPart("registration_number", request.RegistrationNumber);
            AddPart("vin_number", request.VinNumber);
            AddPart("colour", request.Colour);
            if (request.Odometer.HasValue) AddPart("odometer", request.Odometer.Value.ToString());
            AddPart("external_picture", request.ExternalPicture);

            var sw = Stopwatch.StartNew();
            var reqSummary = JsonSerializer.Serialize(request);
            _log.LogInformation("OUT POST {Url} (dealership={Dealership}) body: {ReqBody}", url, ctx.DealershipId, reqSummary);

            var response = await client.PostAsync(url, form);
            var body = await response.Content.ReadAsStringAsync();
            sw.Stop();
            LogResponse("CreateVehicle", response, body, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
                return new Result<VehicleDto>($"TDL CreateVehicle (dealership={ctx.DealershipId}, rego='{request.RegistrationNumber}', stock='{request.StockNumber}') returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");

            var dto = JsonSerializer.Deserialize<VehicleDto>(body);
            return new Result<VehicleDto>(dto!);
        }

        public async Task<Result<VehicleDriveDto>> StartDrive(TdlContext ctx, int driveId, int vehicleId)
        {
            var client = CreateClient(ctx);
            var url = $"{_baseUrl}/api/vehicle-event/{driveId}/ezikey-start-drive/";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(vehicleId.ToString()), "vehicle");

            var sw = Stopwatch.StartNew();
            _log.LogInformation("OUT POST {Url} (dealership={Dealership}) form: vehicle={VehicleId}", url, ctx.DealershipId, vehicleId);

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            var response = await client.SendAsync(req);
            var body = await response.Content.ReadAsStringAsync();
            sw.Stop();
            LogResponse("StartDrive", response, body, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
                return new Result<VehicleDriveDto>($"TDL StartDrive({driveId}, vehicle={vehicleId}) returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");

            var dto = JsonSerializer.Deserialize<VehicleDriveDto>(body);
            return new Result<VehicleDriveDto>(dto!);
        }

        public async Task<Result<VehicleDriveDto>> EndDrive(TdlContext ctx, int driveId, int returningOdometer, string returningFuelLevel)
        {
            var client = CreateClient(ctx);
            var url = $"{_baseUrl}/api/vehicle-event/{driveId}/ezikey-end-drive/";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(returningOdometer.ToString()), "returning_odometer");
            form.Add(new StringContent(returningFuelLevel), "returning_fuel_level");

            var sw = Stopwatch.StartNew();
            _log.LogInformation("OUT POST {Url} (dealership={Dealership}) form: returning_odometer={Odo} returning_fuel_level={Fuel}",
                url, ctx.DealershipId, returningOdometer, returningFuelLevel);

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            var response = await client.SendAsync(req);
            var body = await response.Content.ReadAsStringAsync();
            sw.Stop();
            LogResponse("EndDrive", response, body, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
                return new Result<VehicleDriveDto>($"TDL EndDrive({driveId}) returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");

            var dto = JsonSerializer.Deserialize<VehicleDriveDto>(body);
            return new Result<VehicleDriveDto>(dto!);
        }

        private void LogResponse(string op, HttpResponseMessage response, string body, long elapsedMs)
        {
            var status = (int)response.StatusCode;
            var truncated = body.Length > MaxLoggedBodyChars
                ? body.Substring(0, MaxLoggedBodyChars) + $" …[truncated at {MaxLoggedBodyChars} chars]"
                : body;
            var level = status >= 500 ? LogLevel.Error
                      : status >= 400 ? LogLevel.Warning
                      : LogLevel.Information;
            _log.Log(level, "OUT {Op} <- {Status} in {Elapsed}ms body: {ResBody}", op, status, elapsedMs, truncated);
        }
    }
}
