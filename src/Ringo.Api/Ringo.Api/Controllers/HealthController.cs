using Microsoft.AspNetCore.Mvc;

namespace Ringo.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        // GET: health
        [HttpGet()]
        public IActionResult Get()
        {
            return new JsonResult(new { Status = "Ok" });
        }
    }
}