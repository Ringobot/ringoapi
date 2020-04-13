using SpotifyApi.NetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public interface IAccessTokenService
    {
        Task<string> GetAccessToken(string userId);

        Task<bool> HasAccessToken(string userId);

        Task SetAccessToken(string userId, BearerAccessRefreshToken token);
    }
}
