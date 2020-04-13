using Microsoft.AspNetCore.Mvc;
using Ringo.Api.Models;
using Ringo.Api.Services;
using SpotifyApi.NetCore;
using SpotifyApi.NetCore.Authorization;
using System.Net;
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

        public AuthController(
            IUserAccountsService userAccounts,
            IAccessTokenService tokenService,
            IUserStateService userStateService)
        {
            _userAccounts = userAccounts;
            _tokenService = tokenService;
            _userStateService = userStateService;
        }

        [HttpPost("[action]")]
        [Route("auth/authorize")]
        public async Task<Models.AuthorizationResult> Authorize()
        {
            string userId = CookieHelper.GetUserId(HttpContext);

            if (await _tokenService.HasAccessToken(userId)) return new AuthorizationResult { UserId = userId, Authorized = true };

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
            await _tokenService.SetAccessToken(userId, tokens);

            // return an HTML result that posts a message back to the opening window and then closes itself.
            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = $"<html><body><script>window.opener.postMessage(true, \"*\");window.close()</script><p>Spotify Authorization successful. You can close this window now.</p><p>UserId = {userId}.</p><textarea>{tokens.AccessToken}</textarea></body></html>"
            };
        }
    }
}