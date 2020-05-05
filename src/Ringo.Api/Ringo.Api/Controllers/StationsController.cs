using Microsoft.AspNetCore.Mvc;
using Ringo.Api.Models;
using Ringo.Api.Services;
using System.Threading.Tasks;

namespace Ringo.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class StationsController : ControllerBase
    {
        private readonly IStationService _stationService;
        private readonly IUserService _userService;

        public StationsController(IUserService userService, IStationService stationService)
        {
            _userService = userService;
            _stationService = stationService;
        }

        // PUT: station/whkmas/start
        [AuthSpotifyBearer]
        [HttpPut("{id}/start")]
        public async Task<IActionResult> Start(string id)
        {
            var result = await _stationService.Start(CookieHelper.GetUserId(HttpContext), id);
            return new JsonResult(result) { StatusCode = result.Status };
        }

        // PUT: station/whkmas/join
        [AuthSpotifyBearer]
        [HttpPut("{id}/join")]
        public async Task<IActionResult> Join(string id)
        {
            var result = await _stationService.Join(CookieHelper.GetUserId(HttpContext), id);
            return new JsonResult(result) { StatusCode = result.Status };
        }

        // PUT: player/whkmas/owner

        [AuthSpotifyBearer]
        [HttpPut("{id}/owner")]
        public async Task<IActionResult> Owner(string id)
        {
            var result = await _stationService.ChangeOwner(CookieHelper.GetUserId(HttpContext), id);
            return new JsonResult(result) { StatusCode = result.Status };
        }

        [HttpPost()]
        public async Task<IActionResult> Post([FromBody]CreateStation station)
        {
            var result = await _stationService.CreateStation(station);
            return new JsonResult(result) { StatusCode = result.Status };
        }
    }
}
