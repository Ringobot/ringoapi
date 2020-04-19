using Ringo.Api.Data;
using SpotifyApi.NetCore.Authorization;
using System;

namespace Ringo.Api.Models
{
    public class UserAccessToken : CosmosModel
    {
        public UserAccessToken() { }

        public UserAccessToken(string userId, BearerAccessRefreshToken tokens)
        {
            PK = UserId = userId;
            Id = CanonicalId(userId);
            Type = "UserAccessToken";

            RefreshToken = tokens.RefreshToken;
            ResetAccessToken(tokens, DateTimeOffset.UtcNow);
        }

        public string UserId { get; set; }

        internal bool AccessTokenExpired => Expires <= DateTimeOffset.UtcNow;

        public string RefreshToken { get; set; }

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

        internal static string CanonicalId(string id) => $"T:{id}";

        internal void EnforceInvariants()
        {
        }
    }
}
