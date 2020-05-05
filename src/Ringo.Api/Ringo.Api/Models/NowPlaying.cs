using SpotifyApi.NetCore;
using System;

namespace Ringo.Api.Models
{
    public class NowPlaying
    {
        public bool IsPlaying { get; set; }
        public Track Track { get; set; }
        public SpotifyApi.NetCore.Context Context { get; set; }
        public Offset Offset { get; set; }
        public bool RepeatOn { get; set; }
        public bool ShuffleOn { get; set; }
        public Device Device { get; internal set; }
    }
}