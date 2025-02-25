using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class MoodBarGenerator : StandardComponent, IConfigurableComponent
    {
        const int BASS_ERROR_UNKNOWN = -1;

        const int BASS_STREAMPROC_END = -2147483648;

        const int FFT_SIZE = 512;

        static readonly int[] BANDS = new[]
        {
            300,
            4000,
            20000
        };

        const int LOW = 0;

        const int MID = 1;

        const int HIGH = 2;

        public MoodBarCache Cache { get; private set; }

        public IOutput Output { get; private set; }

        public IOutputStreamDataSourceFactory DataSourceFactory { get; private set; }

        public IFFTDataTransformerFactory DataTransformerFactory { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public IntegerConfigurationElement Resolution { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Cache = ComponentRegistry.Instance.GetComponent<MoodBarCache>();
            this.Output = core.Components.Output;
            this.DataSourceFactory = core.Factories.OutputStreamDataSource;
            this.DataTransformerFactory = core.Factories.FFTDataTransformer;
            this.Configuration = core.Components.Configuration;
            this.Resolution = this.Configuration.GetElement<IntegerConfigurationElement>(
                MoodBarGeneratorConfiguration.SECTION,
                MoodBarGeneratorConfiguration.RESOLUTION_ELEMENT
            );
            base.InitializeComponent(core);
        }

        public MoodBarGeneratorData Generate(IOutputStream stream)
        {
            return this.Cache.GetOrCreate(
                stream,
                this.Resolution.Value,
                () =>
                {
                    var data = new MoodBarGeneratorData()
                    {
                        Resolution = this.Resolution.Value,
                        CancellationToken = new CancellationToken(),
                    };
                    this.Allocate(stream, data);
                    this.Dispatch(() => this.Populate(stream, data));
                    return data;
                }
            );
        }

        protected virtual void Allocate(IOutputStream stream, MoodBarGeneratorData data)
        {
            var max = Convert.ToInt32(
                Math.Ceiling(
                    stream.GetDuration(stream.Length).TotalMilliseconds / this.Resolution.Value
                )
            ).ToNearestPower();
            var length = Convert.ToInt32(
                stream.Length / ((FFT_SIZE * 4) * stream.Channels)
            );
            while (length > max)
            {
                length /= 2;
            }
            data.Data = new MoodBarDataElement[length];
            data.Capacity = length;
        }

        protected virtual void Populate(IOutputStream stream, MoodBarGeneratorData data)
        {
            using (var duplicated = this.Output.Duplicate(stream))
            {
                if (duplicated == null)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to duplicate stream for file \"{0}\", cannot generate.", stream.FileName);
                    return;
                }
                var dataSource = this.DataSourceFactory.Create(duplicated);
                var dataTransformer = this.DataTransformerFactory.Create(BANDS);

                Populate(dataSource, dataTransformer, data);

                if (data.Position < data.Capacity)
                {
                    Logger.Write(this, LogLevel.Debug, "Wave form generation for file \"{0}\" failed to complete.", stream.FileName);
                    this.Cache.Remove(stream, data.Resolution);
                    return;
                }
            }

            if (data.CancellationToken.IsCancellationRequested)
            {
                Logger.Write(this, LogLevel.Debug, "Wave form generation for file \"{0}\" was cancelled.", stream.FileName);
                this.Cache.Remove(stream, data.Resolution);
                return;
            }

            data.Update();

            Logger.Write(this, LogLevel.Debug, "Wave form generated for file \"{0}\" with {1} elements: Peak = {2:0.00}", stream.FileName, data.Capacity, data.Peak);

            try
            {
                this.Cache.Save(stream, data);
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to save mood bar data for file \"{0}\": {1}", stream.FileName, e.Message);
            }
        }

        private static void Populate(IOutputStreamDataSource dataSource, IFFTDataTransformer dataTransformer, MoodBarGeneratorData data)
        {
            var visualizationData = new FFTVisualizationData();
            visualizationData.FFTSize = FFT_SIZE;
            visualizationData.Samples = dataSource.GetBuffer(FFT_SIZE);
            visualizationData.Data = new float[1, visualizationData.Samples.Length];
            visualizationData.Peak = new float[1];

            var length = dataSource.GetData(visualizationData.Samples, FFT_SIZE);
            var interval = Math.Max(data.Capacity / 100, 1);
            var values = new float[BANDS.Length];
            var samplesPerValue = (dataSource.Stream.Length / length) / data.Capacity;

            dataSource.GetFormat(out visualizationData.Rate, out visualizationData.Channels, out visualizationData.Format);

            do
            {
                for (var a = 0; a < samplesPerValue; a++)
                {
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

                    for (var b = 0; b < visualizationData.Samples.Length; b++)
                    {
                        visualizationData.Data[0, b] = visualizationData.Samples[b];
                    }
                    dataTransformer.Transform(visualizationData, values, null, null);

                    data.Data[data.Position].Low = Math.Max(data.Data[data.Position].Low, values[LOW]);
                    data.Data[data.Position].Mid = Math.Max(data.Data[data.Position].Mid, values[MID]);
                    data.Data[data.Position].High = Math.Max(data.Data[data.Position].High, values[HIGH]);

                    data.Peak = Math.Max(
                        data.Peak,
                        Math.Max(
                            values[LOW],
                            Math.Max(
                                values[MID],
                                values[HIGH]
                            )
                        )
                    );

                    length = dataSource.GetData(visualizationData.Samples, FFT_SIZE);
                }

                data.Position++;

                if (data.Position % interval == 0)
                {
                    data.Update();
                }

            } while (!data.CancellationToken.IsCancellationRequested);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return MoodBarGeneratorConfiguration.GetConfigurationSections();
        }

        [Serializable]
        public class MoodBarGeneratorData
        {
            public int Resolution;

            public MoodBarDataElement[] Data;

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

            public static readonly MoodBarGeneratorData Empty = new MoodBarGeneratorData();
        }

        [Serializable]
        public struct MoodBarDataElement
        {
            public float Low;

            public float Mid;

            public float High;
        }
    }
}
