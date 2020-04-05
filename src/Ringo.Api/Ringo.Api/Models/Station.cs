using Ringo.Api.Data;

namespace Ringo.Api.Models
{
    public class Station : CosmosModel
    {
        public User Owner { get; internal set; }
        public string SpotifyContextType { get; internal set; }
    }
}