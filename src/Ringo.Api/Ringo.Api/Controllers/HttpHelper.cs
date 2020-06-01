using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace Ringo.Api.Controllers
{
    internal class HttpHelper
    {
        /// Get's the userId cookie and sets one if it does not exist
        internal static string GetUserId(HttpContext context)
        {
            const string UserIdCookieName = "ringobotUserId";
            string id = context.Request.Cookies[UserIdCookieName];
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                context.Response.Cookies.Append(UserIdCookieName, id,
                    new CookieOptions { Expires = DateTime.Now.AddYears(1), SameSite = SameSiteMode.None });
            }

            return id;
        }

        internal static string GetBearerToken(HttpContext context)
        {
            var headers = context.Request.Headers["Authorization"];
            if (!headers.Any()) return null;
            string bearer = headers[0].Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(bearer)) return null;
            if (bearer.Length != 32) return null;
            return bearer;
        }
    }
}
