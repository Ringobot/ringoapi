using SpotifyApi.NetCore.Authorization;

namespace Ringo.Api.Models
{
    public class UserAccessRefreshToken : UserAccessToken
    {
        public UserAccessRefreshToken() { }

        public UserAccessRefreshToken(string userId, BearerAccessRefreshToken tokens) : base(userId, tokens)
        {
            RefreshToken = tokens.RefreshToken;
        }

        public string RefreshToken { get; set; }
    }
}
