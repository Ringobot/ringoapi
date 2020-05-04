using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Ringo.Api.Services;
using System;
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
            var authHeader = context.HttpContext.Request.Headers["Authorization"];
            if (!authHeader.Any())
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

            // if user exists and user has been authorized and token has not expired and token matches bearer => Continue
            var bearer = authHeader[0].Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
            string userId = CookieHelper.GetUserId(context.HttpContext);
            string accessToken = await _tokens.GetRingoAccessToken(userId);
            if (
                accessToken == null || 
                accessToken != bearer)
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

            await next();
        }
    }
}
