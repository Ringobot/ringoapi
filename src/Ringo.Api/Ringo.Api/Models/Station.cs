using Ringo.Api.Data;
using System;

namespace Ringo.Api.Models
{
    public class Station : CosmosModel
    {
        public Station(string id, string name, string ownerUserId)
        {
            PK = Id = CanonicalId(id);
            Name = name;
            OwnerUserId = ownerUserId;
            Type = "Station";
            Version = "3";
        }

        public string OwnerUserId { get; set; }

        public string Name { get; set; }

        public DateTimeOffset StartDateTime { get; internal set; }

        internal static new string CanonicalId(string stationId) => CosmosModel.CanonicalId(stationId);
    }
}