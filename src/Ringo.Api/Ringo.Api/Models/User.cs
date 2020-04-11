using Ringo.Api.Data;
using SpotifyApi.NetCore.Authorization;
using System;

namespace Ringo.Api.Models
{
    public class User : CosmosModel
    {
        public User() { }

        public User(string userId)
        {
            PK = Id = UserId = userId;
        }

        public string UserId { get; set; }

        public BearerAccessRefreshToken Tokens { get; set; }

        public bool Authorized { get; set; }
        
        public DateTimeOffset AccessTokenExpiresBefore { get; set; }

        public bool AccessTokenHasExpired => AccessTokenExpiresBefore <= DateTimeOffset.UtcNow;
    }
}