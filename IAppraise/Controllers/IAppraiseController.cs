using Integrations;
using Integrations.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IAppraise.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IAppraiseController : ControllerBase
    {
        private IIAppraiseApi _iAppraiseApi;

        public IAppraiseController(IIAppraiseApi iAppraiseApi)
        {
            _iAppraiseApi = iAppraiseApi;
        }

        [HttpGet(Name = "GetAllVehicles")]
        public async Task<IEnumerable<VehicleDto>> GetAllVehicles()
        {
            var result = await _iAppraiseApi.GetAllVehicles();
            return result.Value?.Vehicles ?? Enumerable.Empty<VehicleDto>();
        }
    }
}
