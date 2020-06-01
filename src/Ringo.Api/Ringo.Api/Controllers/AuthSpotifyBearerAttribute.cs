using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Ringo.Api.Services;
using System.Linq;
using System.Threading.Tasks;

namespace Ringo.Api.Controllers
{
    // Passive Attributes
    // https://www.devtrends.co.uk/blog/dependency-injection-in-action-filters-in-asp.net-core

    public class AuthSpotifyBearerAttribute : ActionFilterAttribute
    {
    }

    public class AuthSpotifyBearerFilter : IAsyncActionFilter
    {
        //private readonly IUserService _userService;
        private readonly IAccessTokenService _tokens;

        public AuthSpotifyBearerFilter(IUserService userService, IAccessTokenService accessTokenService)
        {
            //_userService = userService;
            _tokens = accessTokenService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Only run if Controller / Action is decorated with [AuthSpotifyBearer]
            var attribute = context.ActionDescriptor.FilterDescriptors
                .Select(x => x.Filter).OfType<AuthSpotifyBearerAttribute>().FirstOrDefault();
            if (attribute == null)
            {
                await next();
                return;
            }

            // if no auth header => Forbidden
            string bearer = HttpHelper.GetBearerToken(context.HttpContext);
            if (bearer == null)
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

            // if user exists and user has been authorized and token has not expired and token matches bearer => Continue
            string userId = HttpHelper.GetUserId(context.HttpContext);
            var ringoToken = await _tokens.GetRingoAccessToken(userId);

            if (
                ringoToken == null ||
                ringoToken.AccessTokenExpired ||
                ringoToken.AccessToken != bearer)
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

            await next();
        }
    }
}
