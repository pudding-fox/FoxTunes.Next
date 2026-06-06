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

        public override void InitializeComponent(ICore core)
        {
            this.Cache = ComponentRegistry.Instance.GetComponent<MoodBarCache>();
            this.DataSourceFactory = core.Factories.OutputStreamDataSource;
            this.DataTransformerFactory = core.Factories.FFTDataTransformer;

            base.InitializeComponent(core);
        }

        public MoodBarGeneratorData Generate(IOutputStream stream, out Task task)
        {
            var data = this.Cache.Get(stream.FileName);

            if (data != null)
            {
                task = null;
                return data;
            }

            data = new MoodBarGeneratorData()
            {
                FileName = stream.FileName
            };

            this.Allocate(stream, data);

            task = this.Populate(stream, data);

            return data;
        }

        protected virtual void Allocate(IOutputStream stream, MoodBarGeneratorData data)
        {
            data.Data = new float[1024, BANDS.Length];
        }

        protected virtual async Task Populate(IOutputStream stream, MoodBarGeneratorData data)
        {
            var dataSource = this.DataSourceFactory.Create(stream);

            var dataTransformer = this.DataTransformerFactory.Create(BANDS);

            await Task.Run(
                () => Populate(dataSource, dataTransformer, data)
            ).ConfigureAwait(false);

            this.Cache.Write(data);

            Logger.Write(
                this,
                LogLevel.Debug,
                "Moodbar generated for file \"{0}\".",
                stream.FileName
            );
        }

        private static void Populate(IOutputStreamDataSource dataSource, IFFTDataTransformer dataTransformer, MoodBarGeneratorData data)
        {
            var visualizationData = new FFTVisualizationData()
            {
                FFTSize = FFT_SIZE,
                Samples = dataSource.GetBuffer(FFT_SIZE)
            };
            visualizationData.Data = new float[1, visualizationData.Samples.Length];

            dataSource.GetFormat(out visualizationData.Rate, out visualizationData.Channels, out visualizationData.Format);

            var values = new float[BANDS.Length];
            var totalPositions = data.Data.GetLength(0);
            var totalSamples = dataSource.Stream.Length;
            var samplesPerValue = Math.Max(totalSamples / totalPositions, 1);
            var processedSamples = default(long);
            var position = default(int);
            var peak = default(float);

            while (true)
            {
                var length = dataSource.GetData(
                    visualizationData.Samples,
                    FFT_SIZE
                );

                switch (length)
                {
                    case _STREAMPROC_END:
                    case _ERROR_UNKNOWN:
                        if (peak > 0f)
                        {
                            for (var y = 0; y < data.Data.GetLength(0); y++)
                            {
                                for (var x = 0; x < data.Data.GetLength(1); x++)
                                {
                                    data.Data[y, x] /= peak;
                                }
                            }
                        }
                        return;
                }

                for (var a = 0; a < visualizationData.Samples.Length; a++)
                {
                    visualizationData.Data[0, a] = visualizationData.Samples[a];
                }

                dataTransformer.Transform(visualizationData, values, null, null);

                position = (int)(processedSamples / samplesPerValue);

                if (position >= totalPositions)
                {
                    position = totalPositions - 1;
                }

                for (var a = 0; a < BANDS.Length; a++)
                {
                    var value = values[a];

                    if (float.IsNaN(value) || value < 0f)
                    {
                        value = 0f;
                    }

                    value = (float)Math.Sqrt(value);
                    value = Math.Max(value, 0.0001f);
                    data.Data[position, a] += value;

                    if (data.Data[position, a] > peak)
                    {
                        peak = data.Data[position, a];
                    }
                }

                processedSamples += length;
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return MoodBarGeneratorConfiguration.GetConfigurationSections();
        }

        [Serializable]
        public class MoodBarGeneratorData
        {
            public string FileName;

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

            public static readonly MoodBarGeneratorData Empty =
                new MoodBarGeneratorData();
        }
    }
}