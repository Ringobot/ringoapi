using SpotifyApi.NetCore;
using System;

namespace Ringo.Api.Services
{
    public class NowPlaying
    {
        public bool IsPlaying { get; set; }
        public Track Track { get; set; }
        public Context Context { get; set; }
        public Offset Offset { get; set; }
        public bool RepeatOn { get; set; }
        public bool ShuffleOn { get; set; }
        public Device Device { get; internal set; }
    }
}