using Ringo.Api.Data;
using SpotifyApi.NetCore.Authorization;

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
    }
}