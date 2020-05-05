using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ringo.Api.Models
{
    public class Player
    {
        public Context Context { get; set; }
        public string Artist { get; set; }
        public string Track { get; set; }
        public int PositionMsAtEpoch { get; set; }
        public DateTimeOffset? Epoch { get; set; }
        public bool IsPlaying { get; set; }
    }
}
