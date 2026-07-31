using Integrations.Auth;
using Integrations.Configuration;
using Integrations.Core;
using Integrations.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Integrations
{
    public interface IIAppraiseApi
    {
        Task<Result<VehicleResponseDto>> GetAllVehicles(
            string? registrationNumber = null,
            string? stockNumber = null,
            CancellationToken ct = default);

        Task<Result<VehicleEventsResponse>> GetAllUnstartedVehicleEvents(
            CancellationToken ct = default);

        Task<Result<VehicleDriveDto>> StartDrive(
            int driveId,
            int vehicleId,
            CancellationToken ct = default);

        Task<Result<VehicleDriveDto>> EndDrive(
            int driveId,
            int? returningOdometer,
            string returningFuelLevel,
            CancellationToken ct = default);

        Task<Result<VehicleDto>> CreateVehicle(
            CreateVehicleRequestDto request,
            CancellationToken ct = default);
    }

    public class IAppraiseApi : IIAppraiseApi
    {
        public const string HttpClientName = "IAppraiseApi";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IIAppraiseAuthenticator _auth;
        private readonly ILogger<IAppraiseApi> _logger;
        private readonly IAppraiseOptions _options;

        public IAppraiseApi(
            IHttpClientFactory httpClientFactory,
            IIAppraiseAuthenticator auth,
            ILogger<IAppraiseApi> logger,
            IOptions<IAppraiseOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _auth = auth;
            _logger = logger;
            _options = options.Value;
        }

        public Task<Result<VehicleResponseDto>> GetAllVehicles(
            string? registrationNumber = null,
            string? stockNumber = null,
            CancellationToken ct = default)
        {
            var query = new List<string> { $"dealership={_options.DefaultDealershipId}" };
            if (!string.IsNullOrWhiteSpace(registrationNumber))
                query.Add($"registration_number={Uri.EscapeDataString(registrationNumber)}");
            if (!string.IsNullOrWhiteSpace(stockNumber))
                query.Add($"stock_number={Uri.EscapeDataString(stockNumber)}");

            var url = $"vehicle/ezikey-list-all-vehicles-at-site/?{string.Join("&", query)}";
            return SendAsync<VehicleResponseDto>(HttpMethod.Get, url, contentFactory: null, ct);
        }

        public Task<Result<VehicleEventsResponse>> GetAllUnstartedVehicleEvents(
            CancellationToken ct = default)
        {
            var url = $"vehicle-event/ezikey-get-all-unstarted-vehicle-events/?dealership={_options.DefaultDealershipId}";
            return SendAsync<VehicleEventsResponse>(HttpMethod.Get, url, contentFactory: null, ct);
        }

        public Task<Result<VehicleDriveDto>> StartDrive(
            int driveId,
            int vehicleId,
            CancellationToken ct = default)
        {
            var url = $"vehicle-event/{driveId}/ezikey-start-drive/";

            HttpContent Factory()
            {
                var form = new MultipartFormDataContent();
                form.Add(new StringContent(vehicleId.ToString()), "vehicle");
                return form;
            }

            return SendAsync<VehicleDriveDto>(HttpMethod.Post, url, Factory, ct);
        }

        public Task<Result<VehicleDriveDto>> EndDrive(
            int driveId,
            int? returningOdometer,
            string returningFuelLevel,
            CancellationToken ct = default)
        {
            var url = $"vehicle-event/{driveId}/ezikey-return-drive/";

            HttpContent Factory()
            {
                var form = new MultipartFormDataContent();
                if (returningOdometer.HasValue)
                    form.Add(new StringContent(returningOdometer.Value.ToString()), "returning_odometer");
                form.Add(new StringContent(returningFuelLevel ?? string.Empty), "returning_fuel_level");
                return form;
            }

            return SendAsync<VehicleDriveDto>(HttpMethod.Post, url, Factory, ct);
        }

        public Task<Result<VehicleDto>> CreateVehicle(
            CreateVehicleRequestDto request,
            CancellationToken ct = default)
        {
            var url = "vehicle/create-ezikey-vehicle/";
            HttpContent Factory() => JsonContent.Create(request);
            return SendAsync<VehicleDto>(HttpMethod.Post, url, Factory, ct);
        }

        private async Task<Result<T>> SendAsync<T>(
            HttpMethod method,
            string url,
            Func<HttpContent>? contentFactory,
            CancellationToken ct)
        {
            var response = await SendOnceAsync(method, url, contentFactory, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogInformation("iAppraise returned 401 for {Method} {Url}, invalidating cached token and retrying.", method, url);
                response.Dispose();
                _auth.Invalidate();
                response = await SendOnceAsync(method, url, contentFactory, ct);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await SafeReadAsync(response, ct);
                _logger.LogWarning("iAppraise {Method} {Url} returned {Status}: {Body}", method, url, (int)response.StatusCode, body);
                return new Result<T>($"iAppraise returned {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            try
            {
                var stream = await response.Content.ReadAsStreamAsync(ct);
                var value = await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: ct);
                if (value is null)
                {
                    return new Result<T>("iAppraise returned empty body.");
                }
                return new Result<T>(value);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize iAppraise response from {Method} {Url}", method, url);
                return new Result<T>("Failed to parse iAppraise response.");
            }
            finally
            {
                response.Dispose();
            }
        }

        private async Task<HttpResponseMessage> SendOnceAsync(
            HttpMethod method,
            string url,
            Func<HttpContent>? contentFactory,
            CancellationToken ct)
        {
            var token = await _auth.GetTokenAsync(ct);
            var http = _httpClientFactory.CreateClient(HttpClientName);

            var req = new HttpRequestMessage(method, url);
            req.Headers.TryAddWithoutValidation("Authorization", $"Token {token}");
            if (contentFactory is not null)
            {
                req.Content = contentFactory();
            }

            return await http.SendAsync(req, ct);
        }

        private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                return await response.Content.ReadAsStringAsync(ct);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
