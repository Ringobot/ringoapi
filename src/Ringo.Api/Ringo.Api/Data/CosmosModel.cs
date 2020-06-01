using Newtonsoft.Json;

namespace Ringo.Api.Data
{
    public abstract class CosmosModel : ICosmosModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("PartitionKey")]
        public string PK { get; set; }

        [JsonProperty("_etag")]
        public string ETag { get; set; }

        public string Type { get; set; }

        public string Version { get; set; }

        protected static string CanonicalId(string id) => id.ToLower();

        protected static string CanonicalPK(string id) => CanonicalId(id);
    }
}
