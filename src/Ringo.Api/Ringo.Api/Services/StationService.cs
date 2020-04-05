using Microsoft.Extensions.Logging;
using Ringo.Api.Data;
using Ringo.Api.Models;
using Scale;
using SpotifyApi.NetCore;
using SpotifyApi.NetCore.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public class StationService : IStationService
    {
        private static readonly string[] SupportedSpotifyItemTypes = new[] { "playlist" };
        
        private readonly ILogger<StationService> _logger;
        private readonly ICosmosData<Station> _data;
        private readonly IPlayerApi _player;


        public StationService(ILogger<StationService> logger, IPlayerApi playerApi, ICosmosData<Station> stationData)
        {
            _logger = logger;
            _player = playerApi;
            _data = stationData;
        }

        public async Task<StationServiceResult> Start(Models.User user, string stationId)
        {
            var station = await _data.Get(stationId, stationId);
            var np = await GetNowPlaying(user);

            if (!np.IsPlaying) return new StationServiceResult { Status = 201, Message = "Waiting for active device" };

            return new StationServiceResult { Status = 200, Message = "Playing" };
        }

        public async Task<StationServiceResult> Join(Models.User user, string stationId)
        {
            var station = await _data.Get(stationId, stationId);

            var ownerNP = await GetNowPlaying(station.Owner);

            if (!ownerNP.IsPlaying) return new StationServiceResult { Status = 403, Message = "Station Owner's device is not active" };

            // ONE
            var np1 = await GetNowPlaying(user);

            if (!np1.IsPlaying) return new StationServiceResult { Status = 201, Message = "Waiting for active device" };

            // SYNC user to owner

            if (!SupportedSpotifyItemTypes.Contains(station.SpotifyContextType))
                throw new NotSupportedException($"\"{station.SpotifyContextType}\" is not a supported Spotify context type");

            await TurnOffShuffleRepeat(user, np1);

            try
            {
                // mute joining player
                await MuteUnmute(user, np1, true);

                // TWO
                await PlayFromOffset(user, station, ownerNP);

                // THREE
                var np2 = await GetNowPlaying(user);

                //CALCULATE ERROR, if greater than 250ms, DO FOUR
                var error = CalculateError(np1, np2);
                if (error > TimeSpan.FromMilliseconds(250))
                {
                    // FOUR
                    // PLAY OFFSET AGAIN
                    await PlayFromOffset(user, station, ownerNP, error);
                }
            }
            finally
            {
                // unmute joining player
                await MuteUnmute(user, np1, false);
            }

            return new StationServiceResult { Status = 200, Message = "Playing" };
        }

        private async Task PlayFromOffset(Models.User user, Station station, NowPlaying ownerNP, TimeSpan error = default)
        {
            if (error.Equals(default)) error = TimeSpan.Zero;

            var positionMs = Convert.ToInt64(ownerNP.Offset.PositionNow().Add(error).TotalMilliseconds);

            // play from offset
            switch (station.SpotifyContextType)
            {
                case "album":
                    await RetryHelper.RetryAsync(
                        () => _player.PlayAlbumOffset(
                            ownerNP.Context.Uri,
                            ownerNP.Track.Id,
                            accessToken: user.Tokens.AccessToken,
                            positionMs: positionMs),
                            logger: _logger);
                    break;

                case "playlist":
                    await RetryHelper.RetryAsync(
                        () => _player.PlayPlaylistOffset(
                            ownerNP.Context.Uri,
                            ownerNP.Track.Id,
                            accessToken: user.Tokens.AccessToken,
                            positionMs: positionMs),
                        logger: _logger);
                    break;

                default: throw new NotSupportedException($"\"{station.SpotifyContextType}\" is not a supported Spotify Context Type");
            }
        }

        private TimeSpan CalculateError(NowPlaying np1, NowPlaying np2)
        {
            var now = DateTimeOffset.UtcNow;
            return np2.Offset.PositionNow(now).Subtract(np1.Offset.PositionNow(now));
        }

        private async Task<NowPlaying> GetNowPlaying(Models.User user)
        {
            //var info = await RetryHelper.RetryAsync(
            //        () => _player.GetCurrentPlaybackInfo(user.Token),
            //        logger: _logger);

            // DateTime has enough fidelity for these timings
            var start = DateTime.UtcNow;
            CurrentPlaybackContext info = await _player.GetCurrentPlaybackInfo(user.Tokens.AccessToken);
            var finish = DateTime.UtcNow;
            var rtt = finish.Subtract(start);

            var np = new NowPlaying
            {
                IsPlaying = info.IsPlaying,
                Context = info.Context,
                Track = info.Item,
                RepeatOn = info.RepeatState != RepeatStates.Off,
                ShuffleOn = info.ShuffleState,
                Device = info.Device
            };

            // Can be null (e.g. If private session is enabled this will be null).
            if (!info.ProgressMs.HasValue) np.IsPlaying = false;

            if (np.IsPlaying)
            {
                np.Offset = new Offset(
                    finish,
                    TimeSpan.FromMilliseconds(info.ProgressMs ?? 0),
                    rtt,
                    TimeSpan.FromMilliseconds(info.Item.DurationMs))
                {
                    ServerFetchTime = DateTimeOffset.FromUnixTimeMilliseconds(info.Timestamp)
                };
            }

            return np;
        }

        private async Task TurnOffShuffleRepeat(Models.User user, NowPlaying np)
        {
            // turn off shuffle and repeat
            if (np.ShuffleOn)
            {
                await _player.Shuffle(false, accessToken: user.Tokens.AccessToken, deviceId: np.Device.Id);
            }

            if (np.RepeatOn)
            {
                await _player.Repeat(RepeatStates.Off, accessToken: user.Tokens.AccessToken, deviceId: np.Device.Id);
            }
        }

        private async Task MuteUnmute(Models.User user, NowPlaying np, bool mute)
        {
            try
            {
                await _player.Volume(mute ? 0 : 100, accessToken: user.Tokens.AccessToken, deviceId: np.Device.Id);
            }
            catch (Exception ex)
            {
                // log and continue
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
