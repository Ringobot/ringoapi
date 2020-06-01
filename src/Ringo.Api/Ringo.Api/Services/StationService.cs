using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Ringo.Api.Data;
using Ringo.Api.Models;
using SpotifyApi.NetCore;
using SpotifyApi.NetCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public class StationService : IStationService
    {
        private static readonly string[] SupportedSpotifyContexts = new[] { "playlist", "album" };

        private readonly ILogger<StationService> _logger;
        private readonly ICosmosData<Station> _data;
        private readonly IPlayerApi _player;
        private readonly IAccessTokenService _tokens;
        private readonly TelemetryClient _telemetry;
        private readonly IPlayerService _playerService;


        public StationService(
            ILogger<StationService> logger,
            ICosmosData<Station> stationData,
            IPlayerApi playerApi,
            IAccessTokenService accessTokenService,
            TelemetryClient telemetryClient,
            IPlayerService playerService)
        {
            _logger = logger;
            _data = stationData;
            _player = playerApi;
            _tokens = accessTokenService;
            _telemetry = telemetryClient;
            _playerService = playerService;
        }

        public async Task<StationServiceResult> Start(string userId, string stationId)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(stationId)) throw new ArgumentNullException(nameof(stationId));

            var result = new StationServiceResult { Logs = new List<MetricLogEntry>() };

            string sId = Station.CanonicalId(stationId);
            var station = await _data.GetOrDefault(sId, sId);
            if (station == null) return new StationServiceResult { Status = 404, Message = $"Station ({stationId}) not found" };

            // Does station have an owner, and is it this user?
            if (station.OwnerUserId != null && station.OwnerUserId != userId)
            {
                result.Message = $"Station ({stationId}) already has another owner. Change owner before starting.";
                result.Status = 500;
                result.Code = StationServiceResult.StationHasOwner;
                return result;
            }

            var np = await GetNowPlaying(userId, result);

            if (!np.IsPlaying)
            {
                result.Message = "User's device is not active";
                result.Status = 202;
                result.Code = StationServiceResult.UserDeviceNotActive;
                return result;
            }

            if (!ContextSupported(np))
            {
                result.Message = $"Spotify playback context ({np.Context?.Type}) is not supported.";
                result.Status = 500;
                result.Code = StationServiceResult.ContextNotSupported;
                return result;

            }

            // set player
            await _playerService.SetPlayer(stationId, userId, np);

            // set owner
            station.OwnerUserId = userId;

            // set startdate
            station.StartDateTime = DateTimeOffset.UtcNow;

            await _data.Replace(station, station.ETag);

            result.Status = 200;
            result.Message = "Playing";
            result.Success = true;
            return result;
        }

        private bool ContextSupported(NowPlaying np)
        {
            if (np.Context == null) return false;
            return SupportedSpotifyContexts.Any(c => c == np.Context.Type);
        }

        public async Task<StationServiceResult> Join(string userId, string stationId)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(stationId)) throw new ArgumentNullException(nameof(stationId));

            var result = new StationServiceResult { Logs = new List<MetricLogEntry>() };

            string sId = Station.CanonicalId(stationId);
            var station = await _data.GetOrDefault(sId, sId);

            if (station == null) return new StationServiceResult { Status = 404, Message = $"Station ({stationId}) not found" };
            
            if (station.OwnerUserId == null) return new StationServiceResult
            {
                Status = 500,
                Message = $"Station ({stationId}) has no owner. Start the station to continue.",
                Code = StationServiceResult.StationHasNoOwner
            };

            var ownerNP = await GetNowPlaying(station.OwnerUserId, result);

            if (!ownerNP.IsPlaying)
            {
                // set player
                await _playerService.SetPlayer(stationId, station.OwnerUserId, ownerNP);

                return new StationServiceResult
                {
                    Status = 202,
                    Message = $"Station ({stationId}) Owner's device is not active",
                    Code = StationServiceResult.StationOwnersDeviceNotActive
                };
            }

            if (!ContextSupported(ownerNP))
            {
                // set player
                await _playerService.SetPlayer(stationId, station.OwnerUserId, ownerNP);

                result.Message = $"Spotify playback context ({ownerNP.Context?.Type}) is not supported.";
                result.Status = 500;
                result.Code = StationServiceResult.ContextNotSupported;
                return result;
            }

            // ONE
            var np1 = await GetNowPlaying(userId, result);

            if (!np1.IsPlaying)
            {
                // set player
                await _playerService.SetPlayer(stationId, userId, np1);

                return new StationServiceResult
                {
                    Status = 202,
                    Message = "User's device is not active",
                    Code = StationServiceResult.UserDeviceNotActive
                };
            }

            // SYNC user to owner
            await TurnOffShuffleRepeat(userId, np1);

            try
            {
                // mute joining player
                await MuteUnmute(userId, np1, true);

                // TWO
                await PlayFromOffset(userId, station, ownerNP, result);

                // THREE
                //CALCULATE ERROR, if greater than 250ms, DO FOUR
                var ownerNP2 = await GetNowPlaying(station.OwnerUserId, result);
                var error = CalculateError(ownerNP2, await GetNowPlaying(userId, result), result);

                if (Math.Abs(error.TotalMilliseconds) > 500)
                {
                    // FOUR
                    // PLAY OFFSET AGAIN
                    await PlayFromOffset(userId, station, ownerNP2, result, error: error);
                    var ownerNP3 = await GetNowPlaying(station.OwnerUserId, result);
                    var error2 = CalculateError(ownerNP3, await GetNowPlaying(userId, result), result);

                    if (Math.Abs(error2.TotalMilliseconds) > 500)
                    {
                        // FIVE
                        // PLAY OFFSET AGAIN
                        await PlayFromOffset(userId, station, ownerNP3, result, error: error2);
                        CalculateError(await GetNowPlaying(station.OwnerUserId, result), await GetNowPlaying(userId, result), result);
                    }
                }
            }
            finally
            {
                // unmute joining player
                await MuteUnmute(userId, np1, false, volume: np1.Device.VolumePercent ?? 100);
            }

            // set Owner.Player
            await _playerService.SetPlayer(stationId, station.OwnerUserId, ownerNP);

            // set User.Player
            await _playerService.SetPlayer(stationId, userId, np1);

            result.Status = 200;
            result.Message = "Playing";
            return result;
        }

        public Task<StationServiceResult> ChangeOwner(string userId, string stationId)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            //// Does station have an owner?
            //if (station.Owner != null)
            //{
            //    // If the station was started/updated within the past (hour) then the owner can't be 
            //    // changed by starting the station.
            //    if (station.StartedDateTime > DateTimeOffset.UtcNow.AddHours(-1))
            //    {

            //    }

            //    // Get owner now playing
            //    var ownerNP = await GetNowPlaying(station.Owner.Id, result);


            //    // Is that owner currently playing?
            //    if (ownerNP.IsPlaying)

            //    // Is the context the same


            //}


            throw new NotImplementedException();
        }

        public async Task<StationServiceResult> CreateStation(string userId, CreateStation station)
        {
            try
            {
                await _data.Create(new Station(station.Id, station.Name, userId));
                return new StationServiceResult { Status = 200, Message = $"Station ({station.Id}) created" };
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

        private async Task PlayFromOffset(
            string userId,
            Station station,
            NowPlaying ownerNP,
            StationServiceResult result,
            TimeSpan error = default)
        {
            if (error.Equals(default)) error = TimeSpan.Zero;

            string token = (await _tokens.GetSpotifyAccessToken(userId, refreshIfExpired: true)).AccessToken;

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

            var props = new Dictionary<string, string>
                {
                    { "UserId", userId },
                    { "UtcNow", DateTimeOffset.UtcNow.ToString() }
                };

            var metrics = new Dictionary<string, double>
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
                };

            _telemetry.TrackEvent(
                $"Ringo.Api.Services.{nameof(StationService)}.{nameof(PlayFromOffset)}",
                properties: props,
                metrics: metrics);

            result.Logs.Add(new MetricLogEntry($"Ringo.Api.Services.{nameof(StationService)}.{nameof(PlayFromOffset)}")
            {
                Metrics = metrics,
                Properties = props
            });
        }

        private TimeSpan CalculateError(NowPlaying np1, NowPlaying np2, StationServiceResult result)
        {
            var now = DateTimeOffset.UtcNow;
            var error = np2.Offset.PositionNow(now).Subtract(np1.Offset.PositionNow(now));
            result.Logs.Add(new MetricLogEntry($"Ringo.Api.Services.{nameof(StationService)}.{nameof(CalculateError)}")
            {
                Metrics = new Dictionary<string, double>
                {
                    {
                        "ErrorMS",
                        error.TotalMilliseconds
                    }
                }
            });
            return error;
        }

        private async Task<NowPlaying> GetNowPlaying(string userId, StationServiceResult result)
        {
            //var info = await RetryHelper.RetryAsync(
            //        () => _player.GetCurrentPlaybackInfo(user.Token),
            //        logger: _logger);
            string token = (await _tokens.GetSpotifyAccessToken(userId, refreshIfExpired: true)).AccessToken;
            var finish = DateTimeOffset.MaxValue;

            var getInfo = new Func<Task<(CurrentPlaybackContext info, TimeSpan rtt)>>(async () =>
            {
                // DateTime has enough fidelity for these timings
                var start = DateTime.UtcNow;
                CurrentPlaybackContext info = await _player.GetCurrentPlaybackInfo(token);
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
                result.Logs.Add(
                    new MetricLogEntry($"Ringo.Api.Services.{nameof(StationService)}.{nameof(GetNowPlaying)}: _player.GetCurrentPlaybackInfo() returned null Item {info}"));

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


                var metrics = new Dictionary<string, double>
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
                    }
                };

                var props = new Dictionary<string, string>
                {
                    { "UserId", userId },
                    { "UtcNow", finish.ToString() },
                    { "ServerFetchTime", np.Offset.ServerFetchTime.ToString() },
                    { "Timestamp", info.Timestamp.ToString() }
                };

                _telemetry.TrackEvent(
                    $"Ringo.Api.Services.{nameof(StationService)}.{nameof(GetNowPlaying)}",
                    properties: props,
                    metrics: metrics);

                result.Logs.Add(new MetricLogEntry($"Ringo.Api.Services.{nameof(StationService)}.{nameof(GetNowPlaying)}")
                {
                    Metrics = metrics,
                    Properties = props
                });
            }

            return np;
        }

        private async Task TurnOffShuffleRepeat(string userId, NowPlaying np)
        {
            string token = (await _tokens.GetSpotifyAccessToken(userId, refreshIfExpired: true)).AccessToken;

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

        private async Task MuteUnmute(string userId, NowPlaying np, bool mute, int volume = 100)
        {
            try
            {
                string token = (await _tokens.GetSpotifyAccessToken(userId, refreshIfExpired: true)).AccessToken;

                await _player.Volume(mute
                    ? 0
                    : volume, accessToken: token, deviceId: np.Device.Id);
            }
            catch (Exception ex)
            {
                // log and continue
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
