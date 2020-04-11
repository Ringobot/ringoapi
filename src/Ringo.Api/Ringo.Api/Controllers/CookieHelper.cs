using Microsoft.AspNetCore.Http;
using System;

namespace Ringo.Api.Controllers
{
    internal class CookieHelper
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
    }
}
