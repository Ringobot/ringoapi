using Ringo.Api.Data;

namespace Ringo.Api.Models
{
    public class Station : CosmosModel
    {
        public Station(string id, string name)
        {
            PK = Id = CanonicalId(id);
            Name = name;
            //SpotifyContextType = contextType;
            Type = "Station";
            Version = "3";
        }

        public User Owner { get; internal set; }
        //public string SpotifyContextType { get; internal set; }
        public string Name { get; set; }

        internal static string CanonicalId(string id) => id.ToLower();
    }
}