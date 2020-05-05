using Microsoft.AspNetCore.Mvc;
using Ringo.Api.Models;
using Ringo.Api.Services;
using SpotifyApi.NetCore;
using SpotifyApi.NetCore.Authorization;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ringo.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserAccountsService _userAccounts;
        private readonly IAccessTokenService _tokenService;
        private readonly IUserStateService _userStateService;
        private readonly IUserService _userService;

        public AuthController(
            IUserAccountsService userAccounts,
            IAccessTokenService tokenService,
            IUserStateService userStateService,
            IUserService userService)
        {
            _userAccounts = userAccounts;
            _tokenService = tokenService;
            _userStateService = userStateService;
            _userService = userService;
        }

        [HttpPost("[action]")]
        [Route("auth/authorize")]
        public async Task<Models.AuthorizationResult> Authorize()
        {
            string userId = CookieHelper.GetUserId(HttpContext);

            //if (await _tokenService.HasAccessToken(userId)) return new AuthorizationResult { UserId = userId, Authorized = true };

            // create a state value and persist it until the callback
            string state = await _userStateService.NewState(userId);

            // generate an Authorization URL for the read and modify playback scopes
            string url = _userAccounts.AuthorizeUrl(state, new[] { "user-read-playback-state", "user-modify-playback-state" });

            return new AuthorizationResult
            {
                UserId = userId,
                Authorized = false,
                AuthorizationUrl = url
            };
        }

        /// Authorization callback from Spotify
        [HttpGet("[action]")]
        [Route("auth/authorize")]
        public async Task<ContentResult> Authorize(
            [FromQuery(Name = "state")] string state = null,
            [FromQuery(Name = "code")] string code = null,
            [FromQuery(Name = "error")] string error = null)
        {
            if (string.IsNullOrEmpty(state))
            {
                // return Test HTML
                return new ContentResult
                {
                    Content = "<form method=\"post\"><input type=\"submit\" value=\"Authorize\" /></form>",
                    ContentType = "text/html",
                    StatusCode = 200
                };
            }

            string userId = CookieHelper.GetUserId(HttpContext);

            // if Spotify returned an error, throw it
            if (error != null) throw new SpotifyApiErrorException(error);

            // Use the code to request a token
            var tokens = await _userAccounts.RequestAccessRefreshToken(code);

            //TODO: check state is valid
            await _userStateService.ValidateState(state, userId);

            // Save the Token
            await _tokenService.SetSpotifyAccessToken(userId, tokens);

            //TODO: Get the Spotify Username

            // Create a User if not exists
            await _userService.CreateUserIfNotExists(userId);

            // Get a Ringo Token
            var ringoToken = await _tokenService.GetRingoAccessToken(userId);

            // return an HTML result that posts a message back to the opening window and then closes itself.
            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = $"<html><body><script>window.opener.postMessage(\"{ userId },{ ringoToken }\", \"*\");window.close()</script></body></html>"
            };
        }
    }
}