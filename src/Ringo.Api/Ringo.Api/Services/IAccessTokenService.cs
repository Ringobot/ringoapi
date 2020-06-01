using Ringo.Api.Models;
using SpotifyApi.NetCore.Authorization;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public interface IAccessTokenService
    {
        Task<UserAccessToken> GetSpotifyAccessToken(string userId);
        
        Task<UserAccessToken> GetSpotifyAccessToken(string userId, bool refreshIfExpired);

        Task<bool> HasRingoAccessToken(string userId);

        Task SetSpotifyAccessToken(string userId, BearerAccessRefreshToken token);

        Task<RingoUserAccessToken> GetRingoAccessToken(string userId);

        Task<RingoUserAccessToken> RefreshRingoAccessToken(string userId, string bearerToken);
    }
}
