using Microsoft.Azure.Cosmos;
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
        private readonly IAccessTokenService _tokens;

        public StationService(
            ILogger<StationService> logger, 
            ICosmosData<Station> stationData,
            IPlayerApi playerApi,
            IAccessTokenService accessTokenService)
        {
            _logger = logger;
            _data = stationData;
            _player = playerApi;
            _tokens = accessTokenService;
        }

        public async Task<StationServiceResult> Start(Models.User user, string stationId)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            
            string sId = Station.CanonicalId(stationId);
            var station = await _data.GetOrDefault(sId, sId);
            if (station == null) return new StationServiceResult { Status = 404, Message = $"Station ({stationId}) not found" };

            var np = await GetNowPlaying(user);

            if (!np.IsPlaying) return new StationServiceResult { Status = 202, Message = "Waiting for active device" };

            return new StationServiceResult { Status = 200, Success = true, Message = "Playing" };
        }

        public async Task<StationServiceResult> Join(Models.User user, string stationId)
        {
            if (user == null) throw new ArgumentNullException(nameof(user)); 
            
            string sId = Station.CanonicalId(stationId);
            var station = await _data.GetOrDefault(sId, sId);

            if (station == null) return new StationServiceResult { Status = 404, Message = $"Station ({stationId}) not found" };

            var ownerNP = await GetNowPlaying(station.Owner);

            if (!ownerNP.IsPlaying) return new StationServiceResult { Status = 403, Message = "Station Owner's device is not active" };

            // ONE
            var np1 = await GetNowPlaying(user);

            if (!np1.IsPlaying) return new StationServiceResult { Status = 201, Message = "Waiting for active device" };

            // SYNC user to owner
            await TurnOffShuffleRepeat(user, np1);

            try
            {
                // mute joining player
                //await MuteUnmute(user, np1, true);

                // TWO
                await PlayFromOffset(user, station, ownerNP);

                // THREE
                var np2 = await GetNowPlaying(user);

                //CALCULATE ERROR, if greater than 250ms, DO FOUR
                var error = CalculateError(np1, np2);
                if (Math.Abs(error.TotalMilliseconds) > 250)
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

        public Task<StationServiceResult> ChangeOwner(Models.User user, string stationId)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            throw new NotImplementedException();
        }

        public async Task<StationServiceResult> CreateStation(Models.User user, CreateStation station)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            try
            {
                await _data.Create(new Station(station.Id, station.Name, user));
                return new StationServiceResult { Status = 204, Message = $"Station ({station}) created" };
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogError(ex, ex.Message);
                return new StationServiceResult
                {
                    Status = (int)System.Net.HttpStatusCode.Conflict,
                    Message = $"Station ({station.Id}) already exists."
                };
            }
        }

        private async Task PlayFromOffset(Models.User user, Station station, NowPlaying ownerNP, TimeSpan error = default)
        {
            if (error.Equals(default)) error = TimeSpan.Zero;
            
            string token = await _tokens.GetAccessToken(user.UserId);

            var positionMs = Convert.ToInt64(ownerNP.Offset.PositionNow().Add(error).TotalMilliseconds);

            // play from offset
            switch (ownerNP.Context.Type)
            {
                case "album":
                    //await _spotify.PlayAlbumOffset(ownerNP.Context.Uri, ownerNP.Track.Id, user, positionMs);

                    await RetryHelper.RetryAsync(
                        () => _player.PlayAlbumOffset(
                            ownerNP.Context.Uri,
                            ownerNP.Track.Id,
                            accessToken: token,
                            positionMs: positionMs),
                            logger: _logger);
                    break;

                case "playlist":
                    await RetryHelper.RetryAsync(
                        () => _player.PlayPlaylistOffset(
                            ownerNP.Context.Uri,
                            ownerNP.Track.Id,
                            accessToken: token,
                            positionMs: positionMs),
                        logger: _logger);
                    break;

                default: throw new NotSupportedException($"\"{ownerNP.Context.Type}\" is not a supported Spotify Context Type");
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
            CurrentPlaybackContext info = await _player.GetCurrentPlaybackInfo(await _tokens.GetAccessToken(user.UserId));

            // GetCurrentPlaybackInfo may return null if no devices are connected :/
            if (info == null) return new NowPlaying { IsPlaying = false };

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
            string token = await _tokens.GetAccessToken(user.UserId);

            // turn off shuffle and repeat
            if (np.ShuffleOn)
            {
                await _player.Shuffle(false, accessToken: token, deviceId: np.Device.Id);
            }

            if (np.RepeatOn)
            {
                await _player.Repeat(RepeatStates.Off, accessToken: token, deviceId: np.Device.Id);
            }
        }

        private async Task MuteUnmute(Models.User user, NowPlaying np, bool mute)
        {
            try
            {
                await _player.Volume(mute ? 0 : 100, accessToken: await _tokens.GetAccessToken(user.UserId), deviceId: np.Device.Id);
            }
            catch (Exception ex)
            {
                // log and continue
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
