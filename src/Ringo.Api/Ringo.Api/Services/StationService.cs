using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Ringo.Api.Data;
using Ringo.Api.Models;
using SpotifyApi.NetCore;
using SpotifyApi.NetCore.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Ringo.Api.Services
{
    public class StationService : IStationService
    {
        private static readonly string[] SupportedSpotifyItemTypes = new[] { "playlist" };

        private readonly ILogger<StationService> _logger;
        private readonly ICosmosData<Station> _data;
        private readonly IPlayerApi _player;
        private readonly IAccessTokenService _tokens;
        private readonly TelemetryClient _telemetry;

        public StationService(
            ILogger<StationService> logger,
            ICosmosData<Station> stationData,
            IPlayerApi playerApi,
            IAccessTokenService accessTokenService,
            TelemetryClient telemetryClient)
        {
            _logger = logger;
            _data = stationData;
            _player = playerApi;
            _tokens = accessTokenService;
            _telemetry = telemetryClient;
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

                    //await RetryHelper.RetryAsync(
                    //() => 
                    await _player.PlayAlbumOffset(
                        ownerNP.Context.Uri,
                        ownerNP.Track.Id,
                        accessToken: token,
                        positionMs: positionMs);
                    break;

                case "playlist":
                    //await RetryHelper.RetryAsync(
                    //() => 
                    await _player.PlayPlaylistOffset(
                        ownerNP.Context.Uri,
                        ownerNP.Track.Id,
                        accessToken: token,
                        positionMs: positionMs);
                    //logger: _logger);
                    break;

                default: throw new NotSupportedException($"\"{ownerNP.Context.Type}\" is not a supported Spotify Context Type");
            }

            _telemetry.TrackEvent(
                $"Ringo.Api.Services.{nameof(StationService)}.{nameof(PlayFromOffset)}",
                properties: new Dictionary<string, string>
                {
                    { "UserId", user.UserId },
                    { "UtcNow", DateTimeOffset.UtcNow.ToString() }
                },
                metrics: new Dictionary<string, double>
                {
                    {
                        "ErrorMS",
                        error.TotalMilliseconds
                    },
                    {
                        "PositionNowSubstractPositionAtEpochMS",
                        ownerNP.Offset.PositionNow().Subtract(ownerNP.Offset.PositionAtEpoch).TotalMilliseconds
                    },
                    {
                        "PositionMs",
                        positionMs
                    }
                });
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

            var finish = DateTimeOffset.MaxValue;

            var getInfo = new Func<Task<(CurrentPlaybackContext info, TimeSpan rtt)>>(async () =>
            {
                // DateTime has enough fidelity for these timings
                var start = DateTime.UtcNow;
                CurrentPlaybackContext info = await _player.GetCurrentPlaybackInfo(await _tokens.GetAccessToken(user.UserId));
                finish = DateTime.UtcNow;
                var rtt = finish.Subtract(start);

                return (info, rtt);
            });

            CurrentPlaybackContext info = null;
            TimeSpan rtt = TimeSpan.Zero;

            // try three times to get Info
            for (int i = 0; i < 3; i++)
            {
                (info, rtt) = await getInfo();

                // GetCurrentPlaybackInfo may return null if no devices are connected :/
                if (info == null) return new NowPlaying { IsPlaying = false };

                if (info.Item != null) break;
                
                _logger.LogWarning(
                    $"Ringo.Api.Services.{nameof(StationService)}.{nameof(GetNowPlaying)}: _player.GetCurrentPlaybackInfo() returned null Item {info}");
                
                await Task.Delay(333);
            }

            if (info.Item == null) throw new InvalidOperationException("Could not Get Now Playing Info");

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

            _telemetry.TrackEvent(
                $"Ringo.Api.Services.{nameof(StationService)}.{nameof(GetNowPlaying)}",
                properties: new Dictionary<string, string>
                {
                    { "UserId", user.UserId },
                    { "UtcNow", finish.ToString() }
                },
                metrics: new Dictionary<string, double>
                {
                    {
                        "RoundTripTimeMS",
                        rtt.TotalMilliseconds
                    },
                    {
                        "EpochSubtractServerFetchTimeMS",
                        np.Offset.Epoch.Subtract(np.Offset.ServerFetchTime.Value).TotalMilliseconds
                    },
                    {
                        "PositionAtEpochSubtractServerTimeMS",
                        np.Offset.PositionAtEpoch.Subtract(TimeSpan.FromMilliseconds(info.ProgressMs ?? 0)).TotalMilliseconds
                    },
                    {
                        "PositionNowSubstractPositionAtEpochMS",
                        np.Offset.PositionNow().Subtract(np.Offset.PositionAtEpoch).TotalMilliseconds
                    }
                });

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
