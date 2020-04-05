using Microsoft.Extensions.Logging;
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
        private readonly IData<Station> _data;

        private static readonly string[] SupportedSpotifyItemTypes = new[] { "playlist" };
        private readonly ILogger<StationService> _logger;
        private readonly IPlayerApi _player;


        public StationService(ILogger<StationService> logger, IPlayerApi playerApi)
        {
            _logger = logger;
            _player = playerApi;
        }

        public async Task<StationServiceResult> Start(User user, string stationId)
        {
            var station = await _data.Get(stationId);
            var np = await GetNowPlaying(user);

            if (!np.IsPlaying) return new StationServiceResult { Status = 201, Message = "Waiting for active device" };

            return new StationServiceResult { Status = 200, Message = "Playing" };
        }

        public async Task<StationServiceResult> Join(User user, string stationId)
        {
            var station = await _data.Get(stationId);

            var ownerNP = await GetNowPlaying(station.Owner);

            if (!ownerNP.IsPlaying) return new StationServiceResult { Status = 403, Message = "Station Owner's device is not active" };

            // ONE
            var np1 = await GetNowPlaying(user);

            if (!np1.IsPlaying) return new StationServiceResult { Status = 201, Message = "Waiting for active device" };

            // SYNC user to owner

            if (!SupportedSpotifyItemTypes.Contains(station.SpotifyContextType))
                throw new NotSupportedException($"\"{station.SpotifyContextType}\" is not a supported Spotify context type");

            await TurnOffShuffleRepeat(user.Token, np1);

            try
            {
                // mute joining player
                await MuteUnmute(user.Token, np1, true);

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
                await MuteUnmute(user.Token, np1, false);
            }

            return new StationServiceResult { Status = 200, Message = "Playing" };
        }

        private async Task PlayFromOffset(User user, Station station, NowPlaying ownerNP, TimeSpan error = default)
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
                            accessToken: user.Token,
                            positionMs: positionMs),
                            logger: _logger);
                    break;

                case "playlist":
                    await RetryHelper.RetryAsync(
                        () => _player.PlayPlaylistOffset(
                            ownerNP.Context.Uri,
                            ownerNP.Track.Id,
                            accessToken: user.Token,
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

        private async Task<NowPlaying> GetNowPlaying(User user)
        {
            //var info = await RetryHelper.RetryAsync(
            //        () => _player.GetCurrentPlaybackInfo(user.Token),
            //        logger: _logger);

            // DateTime has enough fidelity for these timings
            var start = DateTime.UtcNow;
            CurrentPlaybackContext info = await _player.GetCurrentPlaybackInfo(user.Token);
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

        //private async Task<bool> JoinPlaylist(
        //    string query,
        //    string token,
        //    string stationToken,
        //    Station station,
        //    CancellationToken cancellationToken)
        //{
        //    // is the station playing?
        //    // default the position to what was returned by get info
        //    var info = await GetUserNowPlaying(stationToken);

        //    if (
        //        info == null
        //        || !info.IsPlaying
        //        || info.Context == null
        //        || SpotifyUriHelper.NormalizeUri(info.Context.Uri) != SpotifyUriHelper.NormalizeUri(station.SpotifyUri))
        //    {
        //        _logger.LogInformation($"JoinPlaylist: No longer playing station {station}");
        //        _logger.LogDebug($"JoinPlaylist: station.SpotifyUri = {station.SpotifyUri}");
        //        _logger.LogDebug($"JoinPlaylist: info = {JsonConvert.SerializeObject(info)}");
        //        return false;
        //    }

        //    (string itemId, (long positionMs, DateTime atUtc) position) itemPosition = (info.Item?.Id, (info.ProgressMs ?? 0, DateTime.UtcNow));


        //    if (!SupportedSpotifyItemTypes.Contains(station.SpotifyContextType))
        //        throw new NotSupportedException($"\"{station.SpotifyContextType}\" is not a supported Spotify context type");

        //    var offset = await GetOffset(stationToken);

        //    if (offset.success)
        //    {
        //        // reset position to Station position
        //        itemPosition.itemId = offset.itemId;
        //        itemPosition.position = offset.position;
        //    }

        //    await TurnOffShuffleRepeat(token, info);

        //    try
        //    {
        //        // mute joining player
        //        await Volume(token, 0, info.Device.Id);

        //        // play from offset
        //        switch (station.SpotifyContextType)
        //        {
        //            case "album":
        //                await RetryHelper.RetryAsync(
        //                    () => _player.PlayAlbumOffset(
        //                        info.Context.Uri,
        //                        info.Item.Id,
        //                        accessToken: token,
        //                        positionMs: PositionMsNow(itemPosition.position).positionMs),
        //                    logger: _logger,
        //                    cancellationToken: cancellationToken);
        //                break;

        //            case "playlist":
        //                await RetryHelper.RetryAsync(
        //                    () => _player.PlayPlaylistOffset(
        //                        info.Context.Uri,
        //                        info.Item.Id,
        //                        accessToken: token,
        //                        positionMs: PositionMsNow(itemPosition.position).positionMs),
        //                    logger: _logger,
        //                    cancellationToken: cancellationToken);
        //                break;
        //        }

        //        if (offset.success) await SyncJoiningPlayer(stationToken: stationToken, joiningToken: token);

        //    }
        //    finally
        //    {
        //        // unmute joining player
        //        await Volume(token, (int)info.Device.VolumePercent, info.Device.Id);
        //    }

        //    return true;
        //}

        private async Task TurnOffShuffleRepeat(string token, NowPlaying np)
        {
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

        private async Task MuteUnmute(string token, NowPlaying np, bool mute)
        {
            try
            {
                await _player.Volume(mute ? 0 : 100, accessToken: token, deviceId: np.Device.Id);
            }
            catch (Exception ex)
            {
                // log and continue
                _logger.LogError(ex, ex.Message);
            }
        }

        ///// <summary>
        ///// Given a positionMs at a point in time in the past, returns the positionMs now (or at a given nowUtc)
        ///// </summary>

        ///// <summary>
        ///// Given a position at server time in the past, returns the position it will be by the time 
        ///// a request sent now reaches the server.
        ///// </summary>
        ///// <param name="np"></param>
        ///// <returns></returns>
        //private static long PositionNowMs(NowPlaying np)
        //    //(long positionMs, DateTime atUtc) position,
        //    //DateTimeOffset? nowUtc = null)
        //{
        //    //TODO: CHECK THE MATHS
        //    var serverNow = DateTimeOffset.UtcNow.Add(np.Offset.ClientServerLatency);
        //    var offset = np.Offset.ServerPosition;

        //    // if server fetch time was in the past, add how much time has past
        //    if (serverNow > np.Offset.ServerFetchTime) offset = offset.Add(serverNow.Subtract(np.Offset.ServerFetchTime));

        //    // add an adjustment for client-server latency
        //    //offset = offset.Add(np.Offset.ClientServerLatency);

        //    return Convert.ToInt64(offset);
        //    //return (position.positionMs + Convert.ToInt64(now.Subtract(position.atUtc).TotalMilliseconds), now);
        //}

        ///// <summary>
        ///// Returns the difference in milliseconds between two (position, dateTime) tuples.
        ///// </summary>
        //private static long PositionDiff((long positionMs, DateTime atUtc) position1, (long positionMs, DateTime atUtc) position2)
        //{
        //    DateTime epoch1 = position1.atUtc.AddMilliseconds(-position1.positionMs);
        //    DateTime epoch2 = position2.atUtc.AddMilliseconds(-position2.positionMs);
        //    return Convert.ToInt64(epoch2.Subtract(epoch1).TotalMilliseconds);
        //}

        //private async Task SyncJoiningPlayer(string stationToken, string joiningToken)
        //{
        //    // TODO:
        //    // Christian algorithm
        //    //  T + RTT/2
        //    //  Time + RoundTripTime / 2
        //    var joinerNewPositionMs = new Func<(long, DateTime), (long, DateTime), (long, DateTime)>((
        //        (long position, DateTime atUtc) stationPosition,
        //        (long positionMs, DateTime atUtc) joinerPosition) =>
        //    {
        //        // error is positive if joiner lags station
        //        long error = PositionDiff(stationPosition, joinerPosition);

        //        _logger.LogDebug(
        //            $"SyncJoiningPlayer: joinerNewPositionMs: Token = {BotHelper.TokenForLogging(joiningToken)}, error = {error}");

        //        // shift position of joiner relative to time
        //        return (joinerPosition.positionMs + error, joinerPosition.atUtc);
        //    });

        //    var syncJoiner = new Func<(long, DateTime), Task<(bool, long, (long, DateTime))>>(async ((long positionMs, DateTime atUtc) lastPosition) =>
        //    {
        //        (bool success, string itemId, (long positionMs, DateTime currentUtc) position) current = await GetOffset(joiningToken);
        //        if (!current.success) return (false, 0, (0, DateTime.MinValue));

        //        (long positionMs, DateTime atUtc) adjustedPosition = joinerNewPositionMs(lastPosition, current.position);
        //        (long positionMs, DateTime atUtc) newPosition = PositionMsNow(adjustedPosition);
        //        if (newPosition.positionMs < 0) return (false, 0, (0, DateTime.MinValue));

        //        // play @ Station_Playhead_Now + error
        //        await _player.Seek(newPosition.positionMs, accessToken: joiningToken);

        //        long error = PositionDiff(current.position, newPosition);

        //        _logger.LogDebug(
        //            $"SyncJoiningPlayer: syncJoiner: Token = {BotHelper.TokenForLogging(joiningToken)}, Joiner was synced from {current.position} to {newPosition} (error = {error} ms) based on station position of {lastPosition}");

        //        return (true, error, newPosition);
        //    });

        //    //const long errorAdjustmentThresholdMs = 100;

        //    var stationOffset = await GetOffset(stationToken);

        //    (bool success, long error, (long positionMs, DateTime atUtc) newPosition) attempt1
        //        = await syncJoiner(stationOffset.position);

        //    // if attempt as unsuccessful, or the adjusted error was less than errorAdjustmentThresholdMs, return
        //    //if (!attempt1.success || Math.Abs(attempt1.error) <= errorAdjustmentThresholdMs) return;

        //    // do it again
        //    stationOffset = await GetOffset(stationToken);
        //    await syncJoiner(stationOffset.position);
        //}

        ///// <summary>
        ///// Gets the current playing item and playhead position at a point in time, for a given User.
        ///// </summary>
        //protected internal async Task<(bool success, string itemId, (long progressMs, DateTime atUtc) position)> 
        //    GetOffset(User user)
        //{
        //    var token = user.Token;

        //    // Christian algorithm
        //    //  T + RTT/2
        //    //  Time + RoundTripTime / 2

        //    var results = new List<(string itemId, long progressMs, long roundtripMs, DateTime utc)>();

        //    for (int i = 0; i < 3; i++)
        //    {
        //        try
        //        {
        //            var rt = await GetRoundTrip(token);
        //            results.Add(rt);
        //            _logger.LogDebug($"GetOffset: Token = {BotHelper.TokenForLogging(token)}, GetRoundTrip = {rt}");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, $"GetOffset: Try {i + 1} of 3 failed");
        //        }
        //    }

        //    if (results.Any())
        //    {
        //        (string itemId, long progressMs, long roundtripMs, DateTime utc) = results.OrderBy(r => r.roundtripMs).First();
        //        var result = (true, itemId, (progressMs + (roundtripMs / 2), utc));
        //        _logger.LogDebug($"GetOffset: {BotHelper.TokenForLogging(token)}, result = {result}");
        //        return result;
        //    }

        //    return (false, null, (0, DateTime.MinValue));
        //}

        //protected internal virtual async Task<(string itemId, long progressMs, long roundtripMs, DateTime utc)> 
        //    GetRoundTrip(string token)
        //{
        //    // DateTime has enough fidelity for these timings
        //    var start = DateTime.UtcNow;
        //    CurrentPlaybackContext info1 = await _player.GetCurrentPlaybackInfo(token);
        //    var finish = DateTime.UtcNow;
        //    double rtt = finish.Subtract(start).TotalMilliseconds;
        //    return (info1.Item.Id, info1.ProgressMs ?? 0, Convert.ToInt64(rtt), finish);
        //}
    }
}
