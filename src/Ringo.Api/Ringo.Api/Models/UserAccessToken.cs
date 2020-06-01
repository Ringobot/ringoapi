using Ringo.Api.Data;
using SpotifyApi.NetCore.Authorization;
using System;

namespace Ringo.Api.Models
{
    public class UserAccessToken : CosmosModel
    {
        public UserAccessToken() { }

        protected UserAccessToken(string userId, BearerAccessRefreshToken tokens)
        {
            PK = UserId = userId;
            Id = CanonicalId(userId);
            Type = "UserAccessToken";
            Version = "3";
            ResetAccessToken(tokens, DateTimeOffset.UtcNow);
        }

        internal UserAccessToken(UserAccessRefreshToken refreshToken)
        {
            // copy all properties except Refresh Token
            UserId = refreshToken.UserId;
            AccessToken = refreshToken.AccessToken;
            ExpiresIn = refreshToken.ExpiresIn;
            Scope = refreshToken.Scope;
            Expires = refreshToken.Expires;
        }

        public string UserId { get; set; }

        internal bool AccessTokenExpired => Expires <= DateTimeOffset.UtcNow;

        public string AccessToken { get; set; }

        public int ExpiresIn { get; set; }

        public string Scope { get; set; }

        public DateTimeOffset Expires { get; set; }


        internal void ResetAccessToken(BearerAccessToken newToken, DateTimeOffset issuedDateTime)
        {
            AccessToken = newToken.AccessToken;
            ExpiresIn = newToken.ExpiresIn;
            Scope = newToken.Scope;
            Expires = newToken.Expires ?? issuedDateTime.AddSeconds(ExpiresIn);
        }

        internal static new string CanonicalId(string id) => $"T:{id}";

        internal void EnforceInvariants()
        {
        }
    }
}
