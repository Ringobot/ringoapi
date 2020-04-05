using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ringo.Api.Services;

namespace Ringo.Api.Controllers
{
    [Route("api/[controller]")]
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
        [HttpPut("{id}/start")]
        public async Task<IActionResult> Start(string id)
        {
            // TODO: Map Service Result to HTTP Result
            return new JsonResult(await _stationService.Start(await _userService.GetUser(User), id));
        }

        // PUT: station/whkmas/join
        [HttpPut("{id}/join")]
        public async Task<IActionResult> Join(string id)
        {
            return new JsonResult(await _stationService.Join(await _userService.GetUser(User), id));
        }

        // PUT: player/whkmas/owner
        [HttpPut("{id}/owner")]
        public async Task<IActionResult> Owner(string id)
        {
            throw new NotImplementedException();
        }
    }
}
