using Ringo.Api.Data;

namespace Ringo.Api.Models
{
    public class User : CosmosModel
    {
        public User() { }

        public User(string userId)
        {
            PK = Id = UserId = CanonicalId(userId);
            Type = "User";
            Version = "3";
        }

        public string UserId { get; set; }
    }
}