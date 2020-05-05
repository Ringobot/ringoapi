using Ringo.Api.Data;
using System;

namespace Ringo.Api.Models
{
    public class Station : CosmosModel
    {
        public Station(string id, string name)
        {
            PK = Id = CanonicalId(id);
            Name = name;
            Type = "Station";
            Version = "3";
        }

        public string OwnerUserId { get; set; }

        public string Name { get; set; }

        public DateTimeOffset StartDateTime { get; internal set; }

        internal static string CanonicalId(string id) => id.ToLower();
    }
}