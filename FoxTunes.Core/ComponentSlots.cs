using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class ComponentSlots
    {
        public const string None = "00000000-0000-0000-0000-000000000000";

        public const string Configuration = "C6A34BDB-FFDB-46DC-B7F3-D04CCD319C4E";

        public const string Database = "D5A705D6-4553-4FA8-8642-56176F0BCFF4";

        public const string UserInterface = "17B712AC-2291-466D-BF28-7799D34EC5D7";

        public const string Output = "CC3CEC0B-D882-4A4F-BD4D-A0A505CFD9E1";

        public const string MetaData = "7339A28B-94CA-454D-811F-A8EF61663AD2";

        public const string Signaling = "807955B5-616F-4409-8339-0E662767793A";

        public const string ScriptingRuntime = "24ECF207-EC44-4E98-9533-B78B050DCFDA";

        public const string Logger = "4F0E4441-89E9-45A4-9B05-94491ACF0A99";

        public const string Blocked = "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF";

        public static readonly IDictionary<string, string> Lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Configuration", Configuration },
            { "Database", Database },
            { "UserInterface", UserInterface },
            { "Output", Output },
            { "MetaData", MetaData },
            { "Signaling", Signaling },
            { "ScriptingRuntime", ScriptingRuntime },
            { "Logger", Logger }
        };

        public static IEnumerable<string> All
        {
            get
            {
                return Lookup.Values;
            }
        }
    }
}
