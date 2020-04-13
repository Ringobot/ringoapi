using Ringo.Api.Data;

namespace Ringo.Api.Models
{
    public class User : CosmosModel
    {
        public User() { }

        public User(string userId)
        {
            PK = Id = UserId = userId;
            Type = "User";
        }

        public string UserId { get; set; }
    }
}