using Ringo.Api.Data;
using System;

namespace Ringo.Api.Models
{
    public class Player : CosmosModel
    {
        public Player(string stationId, string userId)
        {
            StationId = CanonicalId(stationId);
            PK = CanonicalPK(stationId);
            UserId = CanonicalId(userId);
            Id = CanonicalId(stationId, userId);
            Type = "Player";
            Version = "3";
        }

        public string StationId { get; set; }

        public string UserId { get; set; }

        public Context Context { get; set; }

        public string Artist { get; set; }

        public string Track { get; set; }

        public int DurationMs { get; set; }

        public int PositionAtEpochMs { get; set; }

        public Int64 EpochMs { get; set; }

        public bool IsPlaying { get; set; }

        internal static string CanonicalId(string stationId, string userId)
            => $"{CanonicalId(stationId)}:{CanonicalId(userId)}";

        internal new static string CanonicalPK(string stationId) => CanonicalId(stationId);
    }
}
