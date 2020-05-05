using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ringo.Api.Services
{
    public class StationServiceResult
    {
        public const string StationHasNoOwner = "StationHasNoOwner";
        public const string StationOwnersDeviceNotActive = "StationOwnersDeviceNotActive";
        public const string UserDeviceNotActive = "UserDeviceNotActive";

        public int Status { get; internal set; }
        public string Message { get; internal set; }
        public bool Success { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<MetricLogEntry> Logs { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Code { get; set; }
    }

    public class MetricLogEntry
    {
        public MetricLogEntry(string message) : this()
        {
            Message = message;
        }

        public MetricLogEntry()
        {
            DateTime = DateTimeOffset.UtcNow;
        }

        public DateTimeOffset DateTime { get; internal set; }
        
        public string Message { get; internal set; }

        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, double> Metrics { get; set; }
    }
}