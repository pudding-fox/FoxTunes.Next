using FoxTunes.Interfaces;
using System;
using System.Linq;

namespace FoxTunes
{
    public class BassEncoderSettingsFactory : StandardComponent
    {
        public ICore Core { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            base.InitializeComponent(core);
        }

        public IBassEncoderSettings CreateSettings(string name)
        {
            Logger.Write(this, LogLevel.Debug, "Creating settings for profile: {0}", name);
            var format = ComponentRegistry.Instance.GetComponents<IBassEncoderSettings>().FirstOrDefault(
                settings => string.Equals(settings.Name, name, StringComparison.OrdinalIgnoreCase)
            );
            if (format == null)
            {
                Logger.Write(this, LogLevel.Debug, "Failed to locate settings for profile: {0}", name);
            }
            return format;
        }
    }
}
