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
            Tokens = tokens;
            Type = "UserAccessToken";
        }

        public string UserId { get; set; }

        public BearerAccessRefreshToken Tokens { get; set; }

        public DateTimeOffset AccessTokenExpiresBefore => Tokens.Expires ?? DateTimeOffset.UtcNow.AddSeconds(Tokens.ExpiresIn);

        public bool AccessTokenHasExpired => AccessTokenExpiresBefore <= DateTimeOffset.UtcNow;

        internal void ResetAccessToken(BearerAccessToken newToken)
        {
            Tokens.AccessToken = newToken.AccessToken;
            Tokens.ExpiresIn = newToken.ExpiresIn;
            Tokens.Scope = newToken.Scope;
        }

        internal static string CanonicalId(string id) => $"T:{id}";

        internal void EnforceInvariants()
        {
            if (Tokens == null) throw new InvalidOperationException();
        }
    }
}
