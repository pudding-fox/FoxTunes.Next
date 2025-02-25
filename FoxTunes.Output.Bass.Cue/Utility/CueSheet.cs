using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class CueSheet
    {
        public CueSheet(string fileName, IEnumerable<CueSheetFile> files, IEnumerable<CueSheetTag> tags)
        {
            this.FileName = fileName;
            this.Files = files.ToArray();
            this.Tags = tags.ToArray();
        }

        public string FileName { get; private set; }

        public CueSheetFile[] Files { get; private set; }

        public CueSheetTag[] Tags { get; private set; }

        public TimeSpan Duration
        {
            get
            {
                return TimeSpan.FromMilliseconds(this.Files.Sum(file => file.Duration.TotalMilliseconds));
            }
        }

        public static string GetTrackLength(CueSheetTrack currentTrack, CueSheetTrack nextTrack)
        {
            var currentTrackPosition = CueSheetIndex.ToTimeSpan(currentTrack.Index.Time);
            var nextTrackPosition = CueSheetIndex.ToTimeSpan(nextTrack.Index.Time);
            var trackLength = nextTrackPosition - currentTrackPosition;
            return CueSheetIndex.FromTimeSpan(trackLength);
        }
    }

    public class CueSheetFile
    {
        private CueSheetFile()
        {
            this.TrackPositions = new Lazy<KeyValuePair<CueSheetTrack, int>[]>(() => GetTrackPositions(this.Tracks));
        }

        public CueSheetFile(string path, string format, IEnumerable<CueSheetTrack> tracks) : this()
        {
            this.Path = path;
            this.Format = format;
            this.Tracks = tracks.ToArray();
        }

        public Lazy<KeyValuePair<CueSheetTrack, int>[]> TrackPositions { get; private set; }

        public string Path { get; private set; }

        public string Format { get; private set; }

        public TimeSpan Duration { get; set; }

        public CueSheetTrack[] Tracks { get; private set; }

        public CueSheetTrack GetNextTrack(CueSheetTrack track)
        {
            var tracks = this.TrackPositions.Value;
            for (var a = 0; a < tracks.Length; a++)
            {
                if (object.ReferenceEquals(tracks[a].Key, track))
                {
                    if (a < tracks.Length - 1)
                    {
                        return tracks[a + 1].Key;
                    }
                }
            }
            return null;
        }

        private static KeyValuePair<CueSheetTrack, int>[] GetTrackPositions(CueSheetTrack[] tracks)
        {
            return tracks.Select(
                track =>
                {
                    var position = default(int);
                    if (!int.TryParse(track.Number, out position))
                    {
                        position = tracks.IndexOf(track) + 1;
                    }
                    return new KeyValuePair<CueSheetTrack, int>(track, position);
                }
            ).OrderBy(pair => pair.Value).ToArray();
        }
    }

    public class CueSheetTrack
    {
        public CueSheetTrack(string number, string type, IEnumerable<CueSheetIndex> indexes, IEnumerable<CueSheetTag> tags)
        {
            this.Number = number;
            this.Type = type;
            this.Indexes = indexes.ToArray();
            this.Tags = tags.ToArray();
        }

        public string Number { get; private set; }

        public string Type { get; private set; }

        public CueSheetIndex[] Indexes { get; private set; }

        public CueSheetTag[] Tags { get; private set; }

        public CueSheetIndex Index
        {
            get
            {
                switch (this.Indexes.Length)
                {
                    case 0:
                        return null;
                    case 1:
                        return this.Indexes[0];
                    default:
                        return this.Indexes[1];
                }
            }
        }
    }

    public class CueSheetIndex
    {
        public CueSheetIndex(string position, string time)
        {
            this.Position = position;
            this.Time = time;
        }

        public string Position { get; private set; }

        public string Time { get; private set; }

        public static string FromTimeSpan(TimeSpan time)
        {
            var minutes = (time.Hours * 60) + time.Minutes;
            var seconds = time.Seconds;
            var frames = Convert.ToInt32(time.Milliseconds * 0.075);
            return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, frames);
        }

        public static TimeSpan ToTimeSpan(string time)
        {
            if (string.IsNullOrEmpty(time))
            {
                return TimeSpan.Zero;
            }
            var minutes = default(int);
            var seconds = default(int);
            var milliseconds = default(int);
            var parts = time.Split(':');
            if (parts.Length > 0)
            {
                int.TryParse(parts[0], out minutes);
            }
            if (parts.Length > 1)
            {
                int.TryParse(parts[1], out seconds);
            }
            if (parts.Length > 2)
            {
                var frames = default(int);
                int.TryParse(parts[2], out frames);
                milliseconds = Convert.ToInt32(frames / 0.075);
            }
            return new TimeSpan(0, 0, minutes, seconds, milliseconds);
        }
    }

    public class CueSheetTag
    {
        public CueSheetTag(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        public string Name { get; private set; }

        public string Value { get; private set; }
    }
}
