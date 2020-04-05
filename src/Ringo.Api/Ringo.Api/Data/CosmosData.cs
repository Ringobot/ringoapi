using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ringo.Api.Data
{
    public class CosmosData<T> : ICosmosData<T> where T : ICosmosModel
    {
        private const string CosmosDbIdKey = "CosmosData_DatabaseId";

        protected readonly TelemetryClient _telemetry;
        protected readonly Container _container;

        protected readonly string ModelName;

        public CosmosData(IConfiguration config, TelemetryClient telemetry, CosmosClient cosmos)
        {
            _telemetry = telemetry;

            // Get model name from Type Argument. This is reflection on construction. CosmosData should be a singleton.
            ModelName = GetType().GenericTypeArguments[0].Name;

            // Derived container app setting key name
            var cosmosContainerKey = $"CosmosData_{ModelName}_Container";

            if (string.IsNullOrEmpty(config[CosmosDbIdKey])) throw new MissingConfigurationException(CosmosDbIdKey);
            if (string.IsNullOrEmpty(config[cosmosContainerKey])) throw new MissingConfigurationException(cosmosContainerKey);

            _container = cosmos.GetContainer(config[CosmosDbIdKey], config[cosmosContainerKey]);
        }

        public async Task<T> Create(T item)
        {
            var response = await _container.CreateItemAsync(item);
            TrackEvent($"CosmosData/{ModelName}/Create", response, item);
            return response.Resource;
        }

        public async Task Delete(string id, string pk, string ifMatchETag)
        {
            // Delete if-match eTag
            var response = await _container.DeleteItemAsync<T>(
                id,
                new PartitionKey(pk),
                new ItemRequestOptions
                {
                    IfMatchEtag = ifMatchETag
                });

            TrackEvent($"CosmosData/{ModelName}/Delete", response.RequestCharge);
        }

        public async Task<T> Get(string id, string pk)
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(pk));
            TrackEvent($"CosmosData/{ModelName}/Get", response, response.Resource);
            return response.Resource;
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            QueryDefinition query =
                new QueryDefinition($"SELECT * FROM {_container.Id} c WHERE c.Type = @type")
                .WithParameter("@type", ModelName);

            var iterator = _container.GetItemQueryIterator<T>(query);
            var items = await iterator.ReadNextAsync();
            TrackEvent($"CosmosData/{ModelName}/GetAll", items.RequestCharge);
            return items.Resource;
        }

        public async Task<T> Replace(T item, string ifMatchEtag)
        {
            // Replace if-match eTag
            var response = await _container.ReplaceItemAsync(
                item,
                item.Id,
                new PartitionKey(item.PK),
                new ItemRequestOptions
                {
                    IfMatchEtag = ifMatchEtag
                });
            TrackEvent($"CosmosData/{ModelName}/Replace", response, response.Resource);
            return response.Resource;
        }

        // Private helpers for tracking Cosmos DB metrics and events
        private void TrackEvent(string eventName, double requestCharge)
        {
            _telemetry.TrackEvent(
                eventName,
                metrics: new Dictionary<string, double>
                {
                    { "Cosmos_RequestCharge", requestCharge }
                });
        }

        private void TrackEvent(string eventName, ItemResponse<T> response, T item)
        {
            _telemetry.TrackEvent(
                eventName,
                properties: new Dictionary<string, string>
                    {
                        { "Cosmos_DocumentType", item.Type },
                        { "Cosmos_DocumentId", item.Id },
                        { "Cosmos_DocumentPK", item.PK },
                        { "Cosmos_DocumentETag", response.ETag }
                    },
                metrics: new Dictionary<string, double>
                    {
                        { "Cosmos_RequestCharge", response.RequestCharge }
                    });
        }
    }
}
