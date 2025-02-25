using MD.Net;
using System;
using System.Linq;

namespace FoxTunes
{
    public static class Extensions
    {
        public static ITrack GetTrack(this ITracks tracks, ITrack track)
        {
            return tracks.FirstOrDefault(_track => string.Equals(_track.Id, track.Id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
