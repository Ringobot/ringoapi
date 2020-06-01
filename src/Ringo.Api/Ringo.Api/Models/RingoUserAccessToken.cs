using Ringo.Api.Services;
using System;

namespace Ringo.Api.Models
{
    public class RingoUserAccessToken
    {
        public RingoUserAccessToken(UserAccessToken userAccessToken)
        {
            AccessToken = CryptoHelper.Sha256(userAccessToken.AccessToken);
            AccessTokenExpired = userAccessToken.AccessTokenExpired;
            UserId = userAccessToken.UserId;
            Expires = userAccessToken.Expires;
        }

        public string AccessToken { get; set; }

        public bool AccessTokenExpired { get; set; }

        public string UserId { get; set; }

        public DateTimeOffset Expires { get; set; }
    }
}
