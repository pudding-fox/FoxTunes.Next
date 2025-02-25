using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FoxTunes
{
    public class CddbTextParser
    {
        private const string DTITLE = "DTITLE";

        private const string DARTIST = "DARTIST";

        private const string DALBUM = "DALBUM";

        private static readonly Regex REGEX = new Regex(@"(?<NAME>[a-zA-Z]+)(?<TRACK>\d+)?=(?<VALUE>.+)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        public CddbTextParser(string id, string sequence)
            : this(id, sequence.GetLines())
        {

        }

        public CddbTextParser(string id, IEnumerable<string> sequence)
        {
            this.Id = id;
            this.Vaues = Parse(sequence);
        }

        public string Id { get; private set; }

        public int Count
        {
            get
            {
                return this.Vaues.Count;
            }
        }

        public IDictionary<int, IDictionary<string, string>> Vaues { get; private set; }

        public bool Contains(int track)
        {
            return this.Vaues.ContainsKey(track);
        }

        public IEnumerable<KeyValuePair<string, string>> Get(int track)
        {
            return this.Vaues[track];
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
                var track = default(int);
                if (string.IsNullOrEmpty(match.Groups[2].Value) || !int.TryParse(match.Groups[2].Value, out track))
                {
                    track = -1;
                }
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
            if (string.Equals(name, DTITLE, StringComparison.OrdinalIgnoreCase))
            {
                var parts = value.Split(new[] { " / " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    AddValue(values, track, DARTIST, parts[0]);
                    AddValue(values, track, DALBUM, parts[1]);
                    return;
                }
            }
            LogManager.Logger.Write(typeof(CddbTextParser), LogLevel.Trace, "Got CDDB tag for track {0} => {1} => {2}", track, name, value);
            values[track][name] = value;
        }
    }
}
