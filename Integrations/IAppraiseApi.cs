using Integrations.Core;
using Integrations.Dtos;
using System.Text.Json;


namespace Integrations
{
    public interface IIAppraiseApi
    {
        Task<Result<VehicleResponseDto>> GetAllVehicles();
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

    }
}
