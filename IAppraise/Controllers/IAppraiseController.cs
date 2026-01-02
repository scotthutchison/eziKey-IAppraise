using Microsoft.AspNetCore.Mvc;

namespace IAppraise.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IAppraiseController : ControllerBase
    {
        [HttpGet(Name = "GetAllVehicles")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                
            })
            .ToArray();
        }
    }
}
