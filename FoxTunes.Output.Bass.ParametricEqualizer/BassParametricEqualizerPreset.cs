using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FoxTunes
{
    public static class BassParametricEqualizerPreset
    {
        const string NAME = "NAME";
        const string BAND_32 = "BAND_32";
        const string BAND_64 = "BAND_64";
        const string BAND_125 = "BAND_125";
        const string BAND_250 = "BAND_250";
        const string BAND_500 = "BAND_500";
        const string BAND_1000 = "BAND_1000";
        const string BAND_2000 = "BAND_2000";
        const string BAND_4000 = "BAND_4000";
        const string BAND_8000 = "BAND_8000";
        const string BAND_16000 = "BAND_16000";

        public static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(BassParametricEqualizerPreset).Assembly.Location);
            }
        }

        public static string SystemPresetsFolder
        {
            get
            {
                return Path.Combine(Location, "Presets");
            }
        }

        public static string UserPresetsFolder
        {
            get
            {
                if (Publication.IsPortable)
                {
                    return SystemPresetsFolder;
                }
                else
                {
                    return Path.Combine(Publication.StoragePath, "Presets");
                }
            }
        }

        public static readonly string PRESET_NONE = Strings.BassParametricEqualizerPreset_None;

        public static readonly IDictionary<string, IDictionary<string, int>> PRESETS = LoadPresets();

        private static IDictionary<string, IDictionary<string, int>> LoadPresets()
        {
            var presets = new Dictionary<string, IDictionary<string, int>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    Strings.BassParametricEqualizerPreset_None,
                    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                    {
                        { BassParametricEqualizerStreamComponentConfiguration.BAND_32, 0 },
                        { BassParametricEqualizerStreamComponentConfiguration.BAND_64, 0 },
                        { BassParametricEqualizerStreamComponentConfiguration.BAND_125, 0 },
                        { BassParametricEqualizerStreamComponentConfiguration.BAND_250, 0 },
                        { BassParametricEqualizerStreamComponentConfiguration.BAND_500, 0 },
                        { BassParametricEqualizerStreamComponentConfiguration.BAND_1000, 0 },
                        { BassParametricEqualizerStreamComponentConfiguration.BAND_2000, 0 },
                        { BassParametricEqualizerStreamComponentConfiguration.BAND_4000, 0 },
                        { BassParametricEqualizerStreamComponentConfiguration.BAND_8000, 0 },
                        { BassParametricEqualizerStreamComponentConfiguration.BAND_16000, 0 }
                    }
                }
            };
            if (Directory.Exists(SystemPresetsFolder))
            {
                Logger.Write(typeof(BassParametricEqualizerPreset), LogLevel.Debug, "Loading system presets..");
                LoadPresets(presets, SystemPresetsFolder);
            }
            if (!string.Equals(SystemPresetsFolder, UserPresetsFolder, StringComparison.OrdinalIgnoreCase))
            {
                if (Directory.Exists(UserPresetsFolder))
                {
                    Logger.Write(typeof(BassParametricEqualizerPreset), LogLevel.Debug, "Loading user presets..");
                    LoadPresets(presets, UserPresetsFolder);
                }
            }
            return presets;
        }

        private static void LoadPresets(IDictionary<string, IDictionary<string, int>> presets, string directoryName)
        {
            foreach (var fileName in Directory.GetFiles(directoryName, "*.txt"))
            {
                try
                {
                    LoadPreset(presets, fileName);
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(BassParametricEqualizerPreset), LogLevel.Warn, "Failed to load preset from file \"{0}\": {1}", fileName, e.Message);
                }
            }
        }

        private static void LoadPreset(IDictionary<string, IDictionary<string, int>> presets, string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            var bands = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { BassParametricEqualizerStreamComponentConfiguration.BAND_32, 0 },
                { BassParametricEqualizerStreamComponentConfiguration.BAND_64, 0 },
                { BassParametricEqualizerStreamComponentConfiguration.BAND_125, 0 },
                { BassParametricEqualizerStreamComponentConfiguration.BAND_250, 0 },
                { BassParametricEqualizerStreamComponentConfiguration.BAND_500, 0 },
                { BassParametricEqualizerStreamComponentConfiguration.BAND_1000, 0 },
                { BassParametricEqualizerStreamComponentConfiguration.BAND_2000, 0 },
                { BassParametricEqualizerStreamComponentConfiguration.BAND_4000, 0 },
                { BassParametricEqualizerStreamComponentConfiguration.BAND_8000, 0 },
                { BassParametricEqualizerStreamComponentConfiguration.BAND_16000, 0 }
            };
            var add = new Action<string, string>((band, value) =>
            {
                var numeric = default(int);
                int.TryParse(value, out numeric);
                numeric = Convert.ToInt32(
                    Math.Max(Math.Min(numeric, PeakEQ.MAX_GAIN), PeakEQ.MIN_GAIN)
                );
                bands[band] = numeric;
            });
            var handlers = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { NAME, value => name = value },
                { string.Format("{0}.{1}", NAME, CultureInfo.CurrentCulture.TwoLetterISOLanguageName), value => name = value },
                { BAND_32, value =>  add(BassParametricEqualizerStreamComponentConfiguration.BAND_32, value) },
                { BAND_64, value =>  add(BassParametricEqualizerStreamComponentConfiguration.BAND_64, value) },
                { BAND_125, value =>  add(BassParametricEqualizerStreamComponentConfiguration.BAND_125, value) },
                { BAND_250, value =>  add(BassParametricEqualizerStreamComponentConfiguration.BAND_250, value) },
                { BAND_500, value =>  add(BassParametricEqualizerStreamComponentConfiguration.BAND_500, value) },
                { BAND_1000, value =>  add(BassParametricEqualizerStreamComponentConfiguration.BAND_1000, value) },
                { BAND_2000, value =>  add(BassParametricEqualizerStreamComponentConfiguration.BAND_2000, value) },
                { BAND_4000, value =>  add(BassParametricEqualizerStreamComponentConfiguration.BAND_4000, value) },
                { BAND_8000, value =>  add(BassParametricEqualizerStreamComponentConfiguration.BAND_8000, value) },
                { BAND_16000, value =>  add(BassParametricEqualizerStreamComponentConfiguration.BAND_16000, value) },
            };
            var lines = File.ReadAllLines(fileName);
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    //Empty line.
                    continue;
                }
                var parts = line.Split('=');
                if (parts.Length != 2)
                {
                    //Unrecognized format.
                    continue;
                }
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                var handler = default(Action<string>);
                if (handlers.TryGetValue(key, out handler))
                {
                    handler(value);
                }
                else
                {
                    Logger.Write(typeof(BassParametricEqualizerPreset), LogLevel.Warn, "Warning while loading preset from file \"{0}\": Unrecognized expression: {1} = {2}", fileName, key, value);
                }
            }
            presets[name] = bands;
            Logger.Write(typeof(BassParametricEqualizerPreset), LogLevel.Debug, "Loaded preset from file \"{0}\".", fileName);
        }

        public static void SavePreset(string name)
        {
            var fileName = Path.Combine(UserPresetsFolder, string.Format("{0}.txt", name.Replace(Path.GetInvalidFileNameChars(), '_')));
            using (var writer = File.CreateText(fileName))
            {
                var write = new Action<string, string>((key, value) => writer.WriteLine("{0}={1}", key, value));
                var handlers = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    { BassParametricEqualizerStreamComponentConfiguration.BAND_32, value =>  write(BAND_32, value) },
                    { BassParametricEqualizerStreamComponentConfiguration.BAND_64, value =>  write(BAND_64, value) },
                    { BassParametricEqualizerStreamComponentConfiguration.BAND_125, value =>  write(BAND_125, value) },
                    { BassParametricEqualizerStreamComponentConfiguration.BAND_250, value =>  write(BAND_250, value) },
                    { BassParametricEqualizerStreamComponentConfiguration.BAND_500, value =>  write(BAND_500, value) },
                    { BassParametricEqualizerStreamComponentConfiguration.BAND_1000, value =>  write(BAND_1000, value) },
                    { BassParametricEqualizerStreamComponentConfiguration.BAND_2000, value =>  write(BAND_2000, value) },
                    { BassParametricEqualizerStreamComponentConfiguration.BAND_4000, value =>  write(BAND_4000, value) },
                    { BassParametricEqualizerStreamComponentConfiguration.BAND_8000, value =>  write(BAND_8000, value) },
                    { BassParametricEqualizerStreamComponentConfiguration.BAND_16000, value =>  write(BAND_16000, value) },
                };
                write(NAME, name);
                write(string.Format("{0}.{1}", NAME, CultureInfo.CurrentCulture.TwoLetterISOLanguageName), name);
                foreach (var band in BassParametricEqualizerStreamComponentConfiguration.Bands)
                {
                    var element = StandardComponents.Instance.Configuration.GetElement<DoubleConfigurationElement>(
                        BassOutputConfiguration.SECTION,
                        band.Key
                    );
                    if (element == null)
                    {
                        continue;
                    }
                    var handler = default(Action<string>);
                    if (handlers.TryGetValue(element.Id, out handler))
                    {
                        handler(Convert.ToString(Convert.ToInt32(element.Value)));
                    }
                    else
                    {
                        //TODO: The setting was not recognized?
                    }
                }
            }
            try
            {
                LoadPreset(PRESETS, fileName);
            }
            catch (Exception e)
            {
                Logger.Write(typeof(BassParametricEqualizerPreset), LogLevel.Warn, "Failed to load preset from file \"{0}\": {1}", fileName, e.Message);
            }
        }

        public static IEnumerable<string> Presets
        {
            get
            {
                return PRESETS.Keys;
            }
        }

        public static string Preset
        {
            get
            {
                foreach (var preset in PRESETS)
                {
                    var active = true;
                    foreach (var band in preset.Value)
                    {
                        var element = StandardComponents.Instance.Configuration.GetElement<DoubleConfigurationElement>(
                            BassOutputConfiguration.SECTION,
                            band.Key
                        );
                        if (element.Value != band.Value)
                        {
                            active = false;
                            break;
                        }
                    }
                    if (active)
                    {
                        return preset.Key;
                    }
                }
                return PRESET_NONE;
            }
            set
            {
                var bands = default(IDictionary<string, int>);
                if (!PRESETS.TryGetValue(value, out bands))
                {
                    return;
                }
                foreach (var band in BassParametricEqualizerStreamComponentConfiguration.Bands)
                {
                    var element = StandardComponents.Instance.Configuration.GetElement<DoubleConfigurationElement>(
                        BassOutputConfiguration.SECTION,
                        band.Key
                    );
                    if (element == null)
                    {
                        continue;
                    }
                    element.Value = bands[band.Key];
                }
            }
        }
    }
}
