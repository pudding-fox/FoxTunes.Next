using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class MoodBarGenerator : StandardComponent, IConfigurableComponent
    {
        const int _ERROR_UNKNOWN = -1;

        const int _STREAMPROC_END = -2147483648;

        const int FFT_SIZE = 2048;

        public static readonly int[] BANDS = new[]
        {
            60,
            120,
            250,
            500,
            1000,
            2000,
            4000,
            8000,
            12000
        };

        public MoodBarCache Cache { get; private set; }

        public IOutputStreamDataSourceFactory DataSourceFactory { get; private set; }

        public IFFTDataTransformerFactory DataTransformerFactory { get; private set; }

        public IConfigurationBase Configuration { get; private set; }

        public IntegerConfigurationElement Resolution { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Cache = ComponentRegistry.Instance.GetComponent<MoodBarCache>();
            this.DataSourceFactory = core.Factories.OutputStreamDataSource;
            this.DataTransformerFactory = core.Factories.FFTDataTransformer;
            this.Configuration = core.Components.Configuration;
            this.Resolution = this.Configuration.GetElement<IntegerConfigurationElement>(
                MoodBarGeneratorConfiguration.SECTION,
                MoodBarGeneratorConfiguration.RESOLUTION
            );
            base.InitializeComponent(core);
        }

        public MoodBarGeneratorData Generate(IOutputStream stream, out Task task)
        {
            var data = this.Cache.Get(stream.FileName, this.Resolution.Value);
            if (data != null)
            {
                task = null;
                return data;
            }
            else
            {
                data = new MoodBarGeneratorData()
                {
                    FileName = stream.FileName,
                    Resolution = this.Resolution.Value
                };
                this.Allocate(stream, data);
                task = this.Populate(stream, data);
                return data;
            }
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
            data.Data = new float[length, BANDS.Length];
        }

        protected virtual async Task Populate(IOutputStream stream, MoodBarGeneratorData data)
        {
            var dataSource = this.DataSourceFactory.Create(stream);
            var dataTransformer = this.DataTransformerFactory.Create(BANDS);

            await Task.Run(() => Populate(dataSource, dataTransformer, data)).ConfigureAwait(false);

            this.Cache.Save(data);

            Logger.Write(this, LogLevel.Debug, "Moodbar generated for file \"{0}\".", stream.FileName);
        }

        private static void Populate(IOutputStreamDataSource dataSource, IFFTDataTransformer dataTransformer, MoodBarGeneratorData data)
        {
            var visualizationData = new FFTVisualizationData();
            visualizationData.FFTSize = FFT_SIZE;
            visualizationData.Samples = dataSource.GetBuffer(FFT_SIZE);
            visualizationData.Data = new float[1, visualizationData.Samples.Length];

            var length = dataSource.GetData(visualizationData.Samples, FFT_SIZE);
            var values = new float[BANDS.Length];
            var samplesPerValue = (dataSource.Stream.Length / length) / data.Data.GetLength(0);
            var position = default(int);

            dataSource.GetFormat(out visualizationData.Rate, out visualizationData.Channels, out visualizationData.Format);

            do
            {
                var samples = default(int);
                for (var a = 0; a < samplesPerValue; a++)
                {
                    switch (length)
                    {
                        case _STREAMPROC_END:
                        case _ERROR_UNKNOWN:
                            return;
                    }

                    if (position >= data.Data.GetLength(0))
                    {
                        return;
                    }

                    for (var b = 0; b < visualizationData.Samples.Length; b++)
                    {
                        visualizationData.Data[0, b] = visualizationData.Samples[b];
                    }
                    dataTransformer.Transform(visualizationData, values, null, null);

                    for (var b = 0; b < BANDS.Length; b++)
                    {
                        var value = (float)Math.Log10(1 + (values[b] * 10));
                        data.Data[position, b] += value;
                    }

                    length = dataSource.GetData(visualizationData.Samples, FFT_SIZE);
                    samples++;
                }

                if (samples > 0)
                {
                    for (var a = 0; a < BANDS.Length; a++)
                    {
                        data.Data[position, a] /= samples;
                    }
                }

                position++;
            } while (true);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return MoodBarGeneratorConfiguration.GetConfigurationSections();
        }

        [Serializable]
        public class MoodBarGeneratorData
        {
            public string FileName;

            public int Resolution;

            public float[,] Data;

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

            public static readonly MoodBarGeneratorData Empty = new MoodBarGeneratorData();
        }
    }
}
