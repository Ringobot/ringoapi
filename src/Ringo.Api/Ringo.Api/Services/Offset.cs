using System;

namespace Ringo.Api.Services
{
    public class Offset
    {
        public Offset(DateTimeOffset epoch, TimeSpan serverPosition, TimeSpan roundTripTime, TimeSpan duration)
        {
            Epoch = epoch;
            ServerPosition = serverPosition;
            RoundTripTime = roundTripTime;
            Duration = duration;
        }

        /// <summary>
        /// Point of time reference for the purpose of time-shifting this position. 
        /// Currently UTC Now on the Client.
        /// </summary>
        public DateTimeOffset Epoch { get; private set; }

        /// <summary>
        /// The position of the playhead according to the Server (S.T).
        /// </summary>
        public TimeSpan ServerPosition { get; private set; }

        /// <summary>
        /// The roundtrip time for the client-server request for this offset.
        /// </summary>
        public TimeSpan RoundTripTime { get; private set; }

        /// <summary>
        /// The total duration of this Track.
        /// </summary>
        public TimeSpan Duration { get; private set; }

        /// <summary>
        /// The calculated position of the playhead at <see cref="Epoch"/> accounting for <see cref="RoundTripTime"/> (P.T).
        /// </summary>
        public TimeSpan PositionAtEpoch 
        { 
            get
            {
                if (RoundTripTime.TotalMilliseconds > 0) return ServerPosition + (RoundTripTime / 2);
                return ServerPosition;
            }
        }

        /// <summary>
        /// The calculated (timeshifted) position of the playhead now (P.T.Now()).
        /// If 
        /// </summary>
        public TimeSpan PositionNow(DateTimeOffset now = default)
        {
            if (now.Equals(default(DateTime))) now = DateTimeOffset.UtcNow;
            var positionNow = PositionAtEpoch.Add(now.Subtract(Epoch));
            return positionNow > Duration ? Duration : positionNow;
        }

        public bool EndOfTrack => PositionNow() == Duration;

        ///// <summary>
        ///// The time on the client that this request for a position was sent.
        ///// </summary>
        //public DateTimeOffset ClientSentTime { get; set; }

        ///// <summary>
        ///// The time that this position was received.
        ///// </summary>
        //public DateTimeOffset ClientReceivedTime { get; set; }
        
        /// <summary>
        /// The time on the server when <see cref="ServerPosition"/>  was fetched.
        /// </summary>
        public DateTimeOffset? ServerFetchTime{ get; set; }
    }
}