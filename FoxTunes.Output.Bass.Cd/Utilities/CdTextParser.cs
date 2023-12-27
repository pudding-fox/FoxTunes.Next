using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FoxTunes
{
    public class CdTextParser
    {
        private static readonly Regex REGEX = new Regex(@"(?<NAME>[a-zA-Z]+)(?<TRACK>\d+)=(?<VALUE>.+)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        public CdTextParser(IEnumerable<string> sequence)
        {
            this.Vaues = Parse(sequence);
        }

        public int Count
        {
            get
            {
                return this.Vaues.Count;
            }
        }

        /// <summary>
        /// Sometimes CD-TEXT is malformed with tracks starting at 2.
        /// If we have a track 0 then it's ok.
        /// </summary>
        public int Offset
        {
            get
            {
                if (this.Vaues.ContainsKey(0))
                {
                    return 0;
                }
                if (this.Vaues.ContainsKey(1))
                {
                    return 1;
                }
                return 0;
            }
        }

        public IDictionary<int, IDictionary<string, string>> Vaues { get; private set; }

        public bool Contains(int track)
        {
            return this.Vaues.ContainsKey(track + this.Offset);
        }

        public IEnumerable<KeyValuePair<string, string>> Get(int track)
        {
            return this.Vaues[track + this.Offset];
        }

        private static IDictionary<int, IDictionary<string, string>> Parse(IEnumerable<string> sequence)
        {
            var values = new Dictionary<int, IDictionary<string, string>>();
            foreach (var element in sequence)
            {
                var match = REGEX.Match(element);
                if (!match.Success)
                {
                    continue;
                }
                var name = match.Groups[1].Value;
                var track = Convert.ToInt32(match.Groups[2].Value) - 1;
                var value = match.Groups[3].Value;
                AddValue(values, track, name, value);
            }
            return values;
        }

        private static void AddValue(IDictionary<int, IDictionary<string, string>> values, int track, string name, string value)
        {
            if (!values.ContainsKey(track))
            {
                values[track] = new Dictionary<string, string>();
            }
            LogManager.Logger.Write(typeof(CdTextParser), LogLevel.Trace, "Got CD-TEXT tag for track {0} => {1} => {2}", track, name, value);
            values[track][name] = value;
        }
    }
}
