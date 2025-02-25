using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class WaveFormGenerator : StandardComponent, IConfigurableComponent
    {
        const int BASS_ERROR_UNKNOWN = -1;

        const int BASS_STREAMPROC_END = -2147483648;

        public WaveFormCache Cache { get; private set; }

        public IOutput Output { get; private set; }

        public IOutputStreamDataSourceFactory Factory { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public IntegerConfigurationElement Resolution { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Cache = ComponentRegistry.Instance.GetComponent<WaveFormCache>();
            this.Output = core.Components.Output;
            this.Factory = core.Factories.OutputStreamDataSource;
            this.Configuration = core.Components.Configuration;
            this.Resolution = this.Configuration.GetElement<IntegerConfigurationElement>(
                WaveFormGeneratorConfiguration.SECTION,
                WaveFormGeneratorConfiguration.RESOLUTION_ELEMENT
            );
            base.InitializeComponent(core);
        }

        public WaveFormGeneratorData Generate(IOutputStream stream)
        {
            return this.Cache.GetOrCreate(
                stream,
                this.Resolution.Value,
                () =>
                {
                    var data = new WaveFormGeneratorData()
                    {
                        Resolution = this.Resolution.Value,
                        Channels = stream.Channels,
                        CancellationToken = new CancellationToken(),
                    };
                    this.Allocate(stream, data);
                    this.Dispatch(() => this.Populate(stream, data));
                    return data;
                }
            );
        }

        protected virtual void Allocate(IOutputStream stream, WaveFormGeneratorData data)
        {
            var length = Convert.ToInt32(
                Math.Ceiling(
                    stream.GetDuration(stream.Length).TotalMilliseconds / this.Resolution.Value
                )
            );
            data.Data = new WaveFormDataElement[length, stream.Channels];
            data.Capacity = length;
        }

        protected virtual void Populate(IOutputStream stream, WaveFormGeneratorData data)
        {
            using (var duplicated = this.Output.Duplicate(stream))
            {
                if (duplicated == null)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to duplicate stream for file \"{0}\", cannot generate.", stream.FileName);
                    return;
                }
                var dataSource = this.Factory.Create(duplicated);
                switch (duplicated.Format)
                {
                    case OutputStreamFormat.Short:
                        Logger.Write(typeof(WaveFormGenerator), LogLevel.Debug, "Creating 16 bit wave form for file \"{0}\" with resolution of {1}ms", stream.FileName, data.Resolution);
                        PopulateShort(this.Output, dataSource, data);
                        break;
                    case OutputStreamFormat.Float:
                        Logger.Write(typeof(WaveFormGenerator), LogLevel.Debug, "Creating 32 bit wave form for file \"{0}\" with resolution of {1}ms", stream.FileName, data.Resolution);
                        PopulateFloat(this.Output, dataSource, data);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                if (data.Position < data.Capacity)
                {
                    Logger.Write(typeof(WaveFormGenerator), LogLevel.Debug, "Wave form generation for file \"{0}\" failed to complete.", stream.FileName);
                    this.Cache.Remove(stream, data.Resolution);
                    return;
                }
            }

            if (data.CancellationToken.IsCancellationRequested)
            {
                Logger.Write(typeof(WaveFormGenerator), LogLevel.Debug, "Wave form generation for file \"{0}\" was cancelled.", stream.FileName);
                this.Cache.Remove(stream, data.Resolution);
                return;
            }

            data.Update();

            Logger.Write(typeof(WaveFormGenerator), LogLevel.Debug, "Wave form generated for file \"{0}\" with {1} elements: Peak = {2:0.00}", stream.FileName, data.Capacity, data.Peak);

            try
            {
                this.Cache.Save(stream, data);
            }
            catch (Exception e)
            {
                Logger.Write(typeof(WaveFormGenerator), LogLevel.Warn, "Failed to save wave form data for file \"{0}\": {1}", stream.FileName, e.Message);
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return WaveFormGeneratorConfiguration.GetConfigurationSections();
        }

        private static void PopulateShort(IOutput output, IOutputStreamDataSource dataSource, WaveFormGeneratorData data)
        {
            var duration = TimeSpan.FromMilliseconds(data.Resolution);
            var buffer = dataSource.GetBuffer<short>(duration);
            var interval = Math.Max(data.Capacity / 100, 1);

            do
            {
                var length = dataSource.GetData(buffer);
                if (length == 0)
                {
                    continue;
                }

                switch (length)
                {
                    case BASS_STREAMPROC_END:
                    case BASS_ERROR_UNKNOWN:
                        return;
                }

                if (data.Position >= data.Capacity)
                {
                    return;
                }

                length /= sizeof(short);

                for (var a = 0; a < length; a += data.Channels)
                {
                    for (var b = 0; b < data.Channels; b++)
                    {
                        var value = (float)buffer[a + b] / short.MaxValue;
                        data.Data[data.Position, b].Min = Math.Min(data.Data[data.Position, b].Min, value);
                        data.Data[data.Position, b].Max = Math.Max(data.Data[data.Position, b].Max, value);
                        data.Data[data.Position, b].Rms += value * value;
                    }
                }

                for (var a = 0; a < data.Channels; a++)
                {
                    data.Data[data.Position, a].Rms = Convert.ToSingle(
                        Math.Sqrt(data.Data[data.Position, a].Rms / (length / data.Channels))
                    );

                    data.Peak = Math.Max(
                        data.Peak,
                        Math.Max(
                            Math.Abs(data.Data[data.Position, a].Min),
                            Math.Abs(data.Data[data.Position, a].Max)
                        )
                    );
                }

                data.Position++;

                if (data.Position % interval == 0)
                {
                    data.Update();
                }

            } while (!data.CancellationToken.IsCancellationRequested);
        }

        private static void PopulateFloat(IOutput output, IOutputStreamDataSource dataSource, WaveFormGeneratorData data)
        {
            var duration = TimeSpan.FromMilliseconds(data.Resolution);
            var buffer = dataSource.GetBuffer<float>(duration);
            var interval = Math.Max(data.Capacity / 100, 1);

            do
            {
                var length = dataSource.GetData(buffer);
                if (length == 0)
                {
                    continue;
                }

                switch (length)
                {
                    case BASS_STREAMPROC_END:
                    case BASS_ERROR_UNKNOWN:
                        return;
                }

                if (data.Position >= data.Capacity)
                {
                    return;
                }

                length /= sizeof(float);

                for (var a = 0; a < length; a += data.Channels)
                {
                    for (var b = 0; b < data.Channels; b++)
                    {
                        var value = buffer[a + b];
                        data.Data[data.Position, b].Min = Math.Min(data.Data[data.Position, b].Min, value);
                        data.Data[data.Position, b].Max = Math.Max(data.Data[data.Position, b].Max, value);
                        data.Data[data.Position, b].Rms += value * value;
                    }
                }

                for (var a = 0; a < data.Channels; a++)
                {
                    data.Data[data.Position, a].Rms = Convert.ToSingle(
                        Math.Sqrt(data.Data[data.Position, a].Rms / (length / data.Channels))
                    );

                    data.Peak = Math.Max(
                        data.Peak,
                        Math.Max(
                            Math.Abs(data.Data[data.Position, a].Min),
                            Math.Abs(data.Data[data.Position, a].Max)
                        )
                    );
                }

                data.Position++;

                if (data.Position % interval == 0)
                {
                    data.Update();
                }

            } while (!data.CancellationToken.IsCancellationRequested);
        }

        [Serializable]
        public class WaveFormGeneratorData
        {
            public int Resolution;

            public WaveFormDataElement[,] Data;

            public int Channels;

            public int Position;

            public int Capacity;

            public float Peak;

            public void Update()
            {
                if (this.Updated == null)
                {
                    return;
                }
                this.Updated(this, EventArgs.Empty);
            }

            [field: NonSerialized]
            public event EventHandler Updated;

            [field: NonSerialized]
            public CancellationToken CancellationToken;

            public static readonly WaveFormGeneratorData Empty = new WaveFormGeneratorData();
        }

        [Serializable]
        public struct WaveFormDataElement
        {
            public float Min;

            public float Max;

            public float Rms;
        }
    }
}
