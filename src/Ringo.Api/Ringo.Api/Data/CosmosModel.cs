using Newtonsoft.Json;

namespace Ringo.Api.Data
{
    public class CosmosModel : ICosmosModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("PartitionKey")]
        public string PK { get; set; }

        [JsonProperty("_etag")]
        public string ETag { get; set; }

        public string Type { get; set; }
    }
}
