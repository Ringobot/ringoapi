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
        private readonly IUserService _userService;

        public AuthSpotifyBearerFilter(IUserService userService)
        {
            _userService = userService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Only run if Action is decorated with [AuthSpotifyBearer]
            var attribute = context.ActionDescriptor.FilterDescriptors
                .Select(x => x.Filter).OfType<AuthSpotifyBearerAttribute>().FirstOrDefault();

            if (attribute == null)
            {
                await next();
                return;
            }

            var authHeader = context.HttpContext.Request.Headers["Authorization"];
            if (!authHeader.Any())
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

            var bearer = authHeader[0].Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(bearer))
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

            string userId = CookieHelper.GetUserId(context.HttpContext);
            var user = await _userService.GetUser(userId);
            if (user == null)
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

            if (!user.Authorized || user.AccessTokenHasExpired || user.Tokens == null)
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

            await next();
        }
    }
}
