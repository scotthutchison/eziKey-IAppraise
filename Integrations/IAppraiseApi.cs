using Integrations.Core;
using Integrations.Dtos;
using System.Text.Json;


namespace Integrations
{
    public interface IIAppraiseApi
    {
        Task<Result<VehicleResponseDto>> GetAllVehicles();
        Task<Result<VehicleEventsResponse>> GetAllUnstartedVehicleEvents();
        Task<Result<VehicleDriveDto>> StartDrive(int driveId, int vehicleId);
        Task<Result<VehicleDriveDto>> EndDrive(int driveId, int returningOdometer, string returningFuelLevel);
    }
    public class IAppraiseApi : IIAppraiseApi
    {
        private static readonly string Token = "Token 66408c79336dea096b212c6c698d8e1b8c1753b7";


        public async Task<Result<VehicleResponseDto>> GetAllVehicles()
        {

            HttpClient _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", Token);

            string url = "https://www.testdriveloan.com.au/api/vehicle/ezikey-list-all-vehicles-at-site/?dealership=251";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(req);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = new Result<VehicleResponseDto>(JsonSerializer.Deserialize<VehicleResponseDto>(json)!);
                return result;
            }
            else
                return new Result<VehicleResponseDto>("Error occured in authenticating with Shift - Refer to Broos Support for assistance.");
        }

        public async Task<Result<VehicleEventsResponse>> GetAllUnstartedVehicleEvents()
        {

            HttpClient _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", Token);

            string url = "https://www.testdriveloan.com.au/api/vehicle-event/ezikey-get-all-unstarted-vehicle-events/?dealership=251";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(req);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = new Result<VehicleEventsResponse>(JsonSerializer.Deserialize<VehicleEventsResponse>(json)!);
                return result;
            }
            else
                return new Result<VehicleEventsResponse>("Error occured in authenticating with Shift - Refer to Broos Support for assistance.");
        }

        public async Task<Result<VehicleDriveDto>> StartDrive(int driveId, int vehicleId)
        {
            HttpClient _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", Token);

            string url = "https://www.testdriveloan.com.au/api/vehicle-event/" + driveId + "/ezikey-start-drive/";

            // multipart/form-data
            using var form = new MultipartFormDataContent();

            // form field: vehicle
            form.Add(new StringContent(vehicleId.ToString()), "vehicle");

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = form
            };

            var response = await _httpClient.SendAsync(req);

            if (!response.IsSuccessStatusCode)
            {
                return new Result<VehicleDriveDto>(
                    "Error occurred in authenticating with Shift - Refer to Broos Support for assistance.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<VehicleDriveDto>(json);

            return new Result<VehicleDriveDto>(dto!);
        }

        public async Task<Result<VehicleDriveDto>> EndDrive(int driveId, int returningOdometer, string returningFuelLevel)
        {
            HttpClient _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", Token);
            string url = "https://www.testdriveloan.com.au/api/vehicle-event/" + driveId + "/ezikey-end-drive/";

            // multipart/form-data
            using var form = new MultipartFormDataContent();
            // form field: returning_odometer
            form.Add(new StringContent(returningOdometer.ToString()), "returning_odometer");
            // form field: returning_fuel_level
            form.Add(new StringContent(returningFuelLevel), "returning_fuel_level");
            
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = form
            };

            var response = await _httpClient.SendAsync(req);

            if (!response.IsSuccessStatusCode)
            {
                return new Result<VehicleDriveDto>(
                    "Error occurred in authenticating with Shift - Refer to Broos Support for assistance.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<VehicleDriveDto>(json);
            return new Result<VehicleDriveDto>(dto!);
        }
    }
}
