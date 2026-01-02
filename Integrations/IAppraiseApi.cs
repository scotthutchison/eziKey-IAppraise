using Integrations.Core;
using Integrations.Dtos;


namespace Integrations
{
    public interface IIAppraiseApi
    {
    }
    public class IAppraiseApi : IIAppraiseApi
    {
        private static readonly string Token = "Token 66408c79336dea096b212c6c698d8e1b8c1753b7";


        private async Task<Result<List<VehicleDto>>> GetAllVehicles()
        {
           
            HttpClient _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", Token);

            string url = "https://www.testdriveloan.com.au/api/vehicle/ezikey-list-all-vehicles-at-site/?dealership=251";

            var reqValues = new Dictionary<string, string>
            {
                { "client_id", clientId.ToString() },
                { "client_secret", clientSecret },
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };

            
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            var response = await _httpClient.SendAsync(req);

            if (response.IsSuccessStatusCode)
            {
                return new Result<List<VehicleDto>>(JsonConvert.DeserializeObject<List<VehicleDto>>(await response.Content.ReadAsStringAsync()));
            }
            else
                return new Result<Token>("Error occured in authenticating with Shift - Refer to Broos Support for assistance.");
        }

    }
}
