using Microsoft.Extensions.Logging;
using Ringo.Api.Data;
using Ringo.Api.Models;
using System;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly ILogger<StationService> _logger;
        private readonly ICosmosData<Player> _data;

        public PlayerService(
            ILogger<StationService> logger,
            ICosmosData<Player> playerData)
        {
            _logger = logger;
            _data = playerData;
        }

        public async Task<Player> GetPlayer(string stationId, string userId)
        {
            return await _data.GetOrDefault(Player.CanonicalId(stationId, userId), Player.CanonicalPK(stationId));
        }

        public async Task<Player> SetPlayer(string stationId, string userId, NowPlaying np)
        {
            var player = await GetPlayer(stationId, userId);
            bool isNew = false;

            if (player == null)
            {
                isNew = true;
                player = new Player(stationId, userId);
            }

            player.Artist = np.Track?.Artists[0]?.Name;
            player.Track = np.Track?.Name;
            player.DurationMs = np.Track?.DurationMs ?? 0;
            player.IsPlaying = np.IsPlaying;

            if (np.Context == null)
            {
                player.Context = null;
            }
            else
            {
                player.Context = new Context { Type = np.Context?.Type, Uri = np.Context?.Uri };
            }

            if (np.Offset == null)
            {
                player.EpochMs = 0;
                player.PositionAtEpochMs = 0;
            }
            else
            {
                player.EpochMs = Convert.ToInt64(np.Offset.Epoch.ToUnixTimeMilliseconds());
                player.PositionAtEpochMs = Convert.ToInt32(np.Offset.PositionAtEpoch.TotalMilliseconds);
            }

            return isNew ? await _data.Create(player) : await _data.Replace(player, player.ETag);
        }
    }
}
