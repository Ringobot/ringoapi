using Newtonsoft.Json;

namespace Ringo.Api.Models
{
    public class AuthorizationResult
    {
        public string UserId { get; set; }
        public bool Authorized { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AuthorizationUrl { get; set; }
    }
}
