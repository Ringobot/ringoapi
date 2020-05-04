using SpotifyApi.NetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public interface IAccessTokenService
    {
        Task<string> GetSpotifyAccessToken(string userId);

        Task<bool> HasRingoAccessToken(string userId);

        Task SetSpotifyAccessToken(string userId, BearerAccessRefreshToken token);

        Task<string> GetRingoAccessToken(string userId);
    }
}
