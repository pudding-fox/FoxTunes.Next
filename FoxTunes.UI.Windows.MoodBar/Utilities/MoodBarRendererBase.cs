using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace FoxTunes
{
    public class MoodBarRendererBase : RendererBase
    {
        const int CACHE_SIZE = 1024;

        const int TIMEOUT = 1000;

        public static Debouncer<IFileData, MoodBarGenerator.MoodBarGeneratorData> Debouncer1 { get; private set; }

        public static TaskScheduler Scheduler { get; private set; }

        public static CappedDictionary<string, MoodBarGenerator.MoodBarGeneratorData> Store { get; private set; }

        static MoodBarRendererBase()
        {
            Debouncer1 = new Debouncer<IFileData, MoodBarGenerator.MoodBarGeneratorData>(TIMEOUT);
            Scheduler = new TaskScheduler(new ParallelOptions()
            {
                MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount / 2, 1)
            });
            Store = new CappedDictionary<string, MoodBarGenerator.MoodBarGeneratorData>(CACHE_SIZE);
        }

        public static readonly DependencyProperty FileDataProperty = DependencyProperty.Register(
            "FileData",
            typeof(IFileData),
            typeof(MoodBarRendererBase),
            new PropertyMetadata(new PropertyChangedCallback(OnFileDataChanged))
        );

        public static IFileData GetFileData(MoodBarRendererBase source)
        {
            return (IFileData)source.GetValue(FileDataProperty);
        }

        public static void SetFileData(MoodBarRendererBase source, IFileData value)
        {
            source.SetValue(FileDataProperty, value);
        }

        public static void OnFileDataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var renderer = sender as MoodBarRendererBase;
            if (renderer == null)
            {
                return;
            }
            renderer.OnFileDataChanged();
        }

        public MoodBarRendererBase()
        {
            Debouncer2 = new Debouncer(TIMEOUT);
            this.Effect = new BlurEffect()
            {
                Radius = 2
            };
        }

        public IFileData FileData
        {
            get
            {
                return GetFileData(this);
            }
            set
            {
                SetFileData(this, value);
            }
        }

        protected virtual void OnFileDataChanged()
        {
            var task = this.Update(this.FileData);
            if (this.FileDataChanged != null)
            {
                this.FileDataChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler FileDataChanged;

        public Debouncer Debouncer2 { get; private set; }

        public object SyncRoot = new object();

        public MoodBarGenerator.MoodBarGeneratorData GeneratorData { get; private set; }

        public MoodBarRendererData RendererData { get; private set; }

        public ICore Core { get; private set; }

        public MoodBarFactory Factory { get; private set; }

        public MoodBarCache Cache { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IntegerConfigurationElement Tint { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.Factory = ComponentRegistry.Instance.GetComponent<MoodBarFactory>();
            this.Cache = ComponentRegistry.Instance.GetComponent<MoodBarCache>();
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.Configuration = core.Components.Configuration;
            this.Tint = this.Configuration.GetElement<IntegerConfigurationElement>(
                MoodBarGeneratorConfiguration.SECTION,
                MoodBarGeneratorConfiguration.TINT
            );
            this.Tint.ValueChanged += this.OnValueChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            this.Debouncer2.Exec(async () =>
            {
                var rendererData = this.RendererData;
                if (rendererData != null)
                {
                    rendererData.Rendered = false;
                    Logger.Write(this, LogLevel.Debug, "Resetting redered flag.");
                }
                var fileData = default(IFileData);
                await Windows.Invoke(() => fileData = this.FileData).ConfigureAwait(false);
                var task = this.Update(fileData);
            });
        }

        public Task UpdateTask { get; private set; }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            var rendererData = this.RendererData;
            if (rendererData != null)
            {
                rendererData.Rendered = false;
                Logger.Write(this, LogLevel.Debug, "Resetting redered flag.");
            }
        }

        protected virtual async Task Update(IFileData fileData)
        {
            if (this.GeneratorData != null)
            {
                this.GeneratorData.Updated -= this.OnUpdated;
            }
            if (this.UpdateTask != null)
            {
                Scheduler.Cancel(this.UpdateTask);
            }
            if (fileData != null)
            {
                var generatorData = this.Cache.Get(fileData.FileName);
                if (generatorData != null)
                {
                    this.GeneratorData = generatorData;
                    await this.RefreshRendererTarget().ConfigureAwait(false);
                }
                else
                {
                    var created = default(bool);
                    generatorData = Store.GetOrAdd(fileData.FileName, () => new MoodBarGenerator.MoodBarGeneratorData()
                    {
                        FileName = fileData.FileName,
                    }, out created);
                    this.GeneratorData = generatorData;
                    this.GeneratorData.Updated += this.OnUpdated;
                    await this.RefreshRendererTarget().ConfigureAwait(false);
                    if (created)
                    {
                        Debouncer1.Exec((arg1, arg2) =>
                        {
                            this.UpdateTask = Scheduler.StartNew(async () =>
                            {
                                using (var task = new CreateTask(arg1, arg2))
                                {
                                    task.InitializeComponent(this.Core);
                                    await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                                    await task.Run().ConfigureAwait(false);
                                    if (task.IsFaulted)
                                    {
                                        return;
                                    }
                                }
                            });
                        }, new[] { fileData }, new[] { this.GeneratorData });
                    }
                }
            }
            else
            {
                this.GeneratorData = MoodBarGenerator.MoodBarGeneratorData.Empty;
                await this.Clear().ConfigureAwait(false);
            }
        }

        protected virtual void OnUpdated(object sender, EventArgs e)
        {
            this.Dispatch(this.Update);
        }

        protected override bool CreateData(int width, int height)
        {
            var generatorData = this.GeneratorData;
            if (generatorData == null)
            {
                generatorData = MoodBarGenerator.MoodBarGeneratorData.Empty;
            }
            this.RendererData = Create(
                generatorData,
                width,
                height,
                this.GetColorPalettes(this.GetColorPaletteOrDefault(string.Empty))
            );
            if (this.RendererData == null)
            {
                return false;
            }
            this.Dispatch(this.Update);
            return true;
        }

        protected virtual IDictionary<string, IntPtr> GetColorPalettes(string value)
        {
            var flags = default(int);
            var palettes = MoodBarStreamPositionConfiguration.GetColorPalette(value);
            {
                var background = palettes.GetOrAdd(
                    MoodBarStreamPositionConfiguration.COLOR_PALETTE_BACKGROUND,
                    () => DefaultColors.GetBackground()
                );
            }
            return palettes.ToDictionary(
                pair => pair.Key,
                pair =>
                {
                    if (pair.Value == null)
                    {
                        return IntPtr.Zero;
                    }
                    flags = 0;
                    return BitmapHelper.CreatePalette(flags, GetAlphaBlending(pair.Key, pair.Value), pair.Value);
                },
                StringComparer.OrdinalIgnoreCase
            );
        }

        protected override RendererTarget CreateRendererTarget(int width, int height)
        {
            var target = base.CreateRendererTarget(width, height);
            this.ClearRendererTarget(target);
            return target;
        }

        protected override void ClearRendererTarget(RendererTarget target)
        {
            if (!target.TryLock())
            {
                this.Dispatch(() => this.ClearRendererTarget(target));
                return;
            }
            try
            {
                var info = default(BitmapHelper.RenderInfo);
                var data = this.RendererData;
                if (data != null)
                {
                    info = BitmapHelper.CreateRenderInfo(target, data.Colors[MoodBarStreamPositionConfiguration.COLOR_PALETTE_BACKGROUND]);
                }
                else
                {
                    var palettes = this.GetColorPalettes(this.GetColorPaletteOrDefault(string.Empty));
                    info = BitmapHelper.CreateRenderInfo(target, palettes[MoodBarStreamPositionConfiguration.COLOR_PALETTE_BACKGROUND]);
                }
                BitmapHelper.DrawRectangle(ref info, 0, 0, data.Width, data.Height);
                target.Invalidate();
            }
            finally
            {
                target.Unlock();
            }
            this.Dispatch(this.Update);
        }

        public Task Render(MoodBarGenerator.MoodBarGeneratorData generatorData, MoodBarRendererData rendererData)
        {
            return Windows.Invoke(() =>
            {
                var target = this.RendererTarget;
                if (target == null)
                {
                    return;
                }

                if (!target.TryLock())
                {
                    var task = this.Render(generatorData, rendererData);
                    return;
                }
                var info = GetRenderInfo(target, rendererData, this.Tint.Value);
                Monitor.Enter(this.SyncRoot);
                try
                {
                    Render(ref info, generatorData, rendererData);
                    if (rendererData.Rendered)
                    {
                        Logger.Write(this.GetType(), LogLevel.Debug, "Mood bar rendered successfully.");
                        generatorData.Updated -= this.OnUpdated;
                    }
                }
                catch (Exception e)
                {
                    Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render mood bar: {0}", e.Message);
                }
                finally
                {
                    Monitor.Exit(this.SyncRoot);
                }
                target.Invalidate();
                target.Unlock();
            });
        }

        public void Update()
        {
            var generatorData = this.GeneratorData;
            var rendererData = this.RendererData;
            if (generatorData != null && rendererData != null)
            {
                if (rendererData.Rendered)
                {
                    Logger.Write(this, LogLevel.Debug, "Already rendered.");
                    return;
                }
                var task = this.Render(generatorData, rendererData);
            }
        }

        protected override int GetPixelWidth(double width)
        {
            var data = this.GeneratorData;
            if (data != null && data.Data != null)
            {
                var valuesPerElement = Convert.ToInt32(
                    Math.Ceiling(
                        Math.Max(
                            (float)data.Data.GetLength(0) / width,
                            1
                        )
                    )
                );
                width = data.Data.GetLength(0) / valuesPerElement;
            }
            return base.GetPixelWidth(width);
        }

        protected override void OnDisposing()
        {
            if (this.GeneratorData != null)
            {
                this.GeneratorData.Updated -= this.OnUpdated;
            }
            if (this.Tint != null)
            {
                this.Tint.ValueChanged -= this.OnValueChanged;
            }
            base.OnDisposing();
        }

        private static MoodBarRenderInfo GetRenderInfo(RendererTarget target, MoodBarRendererData data, int tint)
        {
            var info = new MoodBarRenderInfo()
            {
                Tint = tint,
                Background = BitmapHelper.CreateRenderInfo(target, data.Colors[MoodBarStreamPositionConfiguration.COLOR_PALETTE_BACKGROUND])
            };
            return info;
        }

        public static void Render(ref MoodBarRenderInfo info, MoodBarGenerator.MoodBarGeneratorData generatorData, MoodBarRendererData rendererData)
        {
            var data = generatorData.Data;

            if (info.Background.Width != rendererData.Width || info.Background.Height != rendererData.Height || data == null)
            {
                return;
            }

            var rows = data.GetLength(0);
            var cols = data.GetLength(1);

            BitmapHelper.DrawRectangle(
                ref info.Background,
                0,
                0,
                rendererData.Width,
                rendererData.Height
            );

            var values = new float[cols];
            var averages = new float[cols];
            var normalization = new float[cols];
            var palettes = new Dictionary<Color, IntPtr>();

            const byte SHADE = 100;
            var contrast = Color.FromRgb(SHADE, SHADE, SHADE);

            try
            {
                for (var a = 0; a < rows; a++)
                {
                    for (var b = 0; b < cols; b++)
                    {
                        averages[b] += data[a, b];
                    }
                }

                for (var a = 0; a < cols; a++)
                {
                    averages[a] /= rows;

                    if (averages[a] <= 0f)
                    {
                        averages[a] = 1f;
                    }

                    normalization[a] = 1f / (float)Math.Pow(averages[a], 0.12f);
                }

                for (var a = 0; a < rows; a++)
                {
                    var b = (int)((a / (float)rows) * rendererData.Width);
                    var c = (int)(((a + 1) / (float)rows) * rendererData.Width);
                    var width = c - b;

                    if (width < 1)
                    {
                        width = 1;
                    }

                    if (b >= rendererData.Width)
                    {
                        continue;
                    }

                    if (b + width > rendererData.Width)
                    {
                        width = rendererData.Width - b;
                    }

                    var max = 0f;
                    for (var d = 0; d < cols; d++)
                    {
                        var value = default(float);
                        if (a > 0 && a < rows - 1)
                        {
                            value = data[a, d];
                        }
                        else
                        {
                            value = data[a, d];
                        }

                        value *= normalization[d];
                        values[d] = value;
                        if (value > max)
                        {
                            max = value;
                        }
                    }

                    var color = MoodBarColorProvider.GetColor(values, info.Tint);
                    {
                        var palette = palettes.GetOrAdd(color, () => BitmapHelper.CreatePalette(new[] { new Int32Color(color) }, 1, 0));
                        var render = BitmapHelper.CreateRenderInfo(info.Background, palette);
                        BitmapHelper.DrawRectangle(ref render, b, 0, width, rendererData.Height);
                    }

                    var shaded = color.Shade(contrast);
                    var y = (int)((rendererData.Height * 0.5f) - (max * (rendererData.Height * 0.5f)));
                    var height = (int)(((rendererData.Height * 0.5f) - y) + (max * (rendererData.Height * 0.5f)));
                    if (height < 1)
                    {
                        height = 1;
                    }

                    {
                        var palette = palettes.GetOrAdd(shaded, () => BitmapHelper.CreatePalette(new[] { new Int32Color(shaded) }, 1, 0));
                        var render = BitmapHelper.CreateRenderInfo(info.Background, palette);
                        BitmapHelper.DrawRectangle(ref render, b, y, width, height);
                    }
                }
            }
            finally
            {
                foreach (var pair in palettes)
                {
                    var palette = pair.Value;
                    BitmapHelper.DestroyPalette(ref palette);
                }
            }

            rendererData.Rendered = true;
        }

        private static float Smooth(float[,] data, int a, int d, int radius)
        {
            if (radius <= 0)
                return data[a, d];

            int width = data.GetLength(0);

            float sum = 0f;
            int count = 0;

            for (int offset = -radius; offset <= radius; offset++)
            {
                int index = a + offset;

                // bounds check
                if (index < 0 || index >= width)
                    continue;

                sum += data[index, d];
                count++;
            }

            return sum / count;
        }

        public static MoodBarRendererData Create(MoodBarGenerator.MoodBarGeneratorData generatorData, int width, int height, IDictionary<string, IntPtr> colors)
        {
            var data = new MoodBarRendererData()
            {
                Width = width,
                Height = height,
                Colors = colors
            };
            return data;
        }

        public static Int32Color HsvToRgb(float h, float s, float v)
        {
            while (h < 0) h += 360;
            while (h >= 360) h -= 360;

            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = v - c;

            float r = 0, g = 0, b = 0;

            if (h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return new Int32Color(
                Color.FromRgb(
                    (byte)((r + m) * 255),
                    (byte)((g + m) * 255),
                    (byte)((b + m) * 255)
                )
            );
        }

        public class MoodBarRendererData
        {
            public int Width;

            public int Height;

            public IDictionary<string, IntPtr> Colors;

            public bool Rendered;

            ~MoodBarRendererData()
            {
                try
                {
                    if (this.Colors != null)
                    {
                        foreach (var pair in this.Colors)
                        {
                            var value = pair.Value;
                            BitmapHelper.DestroyPalette(ref value);
                        }
                        this.Colors.Clear();
                    }
                }
                catch
                {
                    //Nothing can be done, never throw on GC thread.
                }
            }
        }

        private class CreateTask : BackgroundTask
        {
            public static readonly IMoodBarFactory MoodBarFactory = ComponentRegistry.Instance.GetComponent<IMoodBarFactory>();

            private CreateTask() : base(Guid.NewGuid().ToString("d")/*Allow concurrency, it is controlled by the scheduler.*/)
            {
            }

            public CreateTask(IEnumerable<IFileData> fileDatas, IEnumerable<MoodBarGenerator.MoodBarGeneratorData> generatorDatas) : this()
            {
                this.FileDatas = fileDatas.ToArray();
                this.MoodBarItems = fileDatas
                    .OrderBy(fileData => fileData.FileName)
                    .Select(fileData => MoodBarItem.FromFileData(fileData))
                    .ToArray();
                this.GeneratorDatas = generatorDatas.ToArray();
            }

            public override bool Visible
            {
                get
                {
                    return true;
                }
            }

            public override bool Cancellable
            {
                get
                {
                    return true;
                }
            }

            public IFileData[] FileDatas { get; private set; }

            public MoodBarItem[] MoodBarItems { get; private set; }

            public MoodBarGenerator.MoodBarGeneratorData[] GeneratorDatas { get; private set; }

            public ICore Core { get; private set; }

            public ISignalEmitter SignalEmitter { get; private set; }

            public IMoodBar MoodBar { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.Core = core;
                this.SignalEmitter = core.Components.SignalEmitter;
                base.InitializeComponent(core);
            }

            protected override async Task OnRun()
            {
                if (this.FileDatas.Any() && this.GeneratorDatas.Any())
                {
                    Logger.Write(this, LogLevel.Debug, "Creating.");
                    using (this.MoodBar = MoodBarFactory.CreateMoodBar(this.MoodBarItems))
                    {
                        Logger.Write(this, LogLevel.Debug, "Starting.");
                        using (var monitor = new MoodBarMonitor(this.MoodBar, this.GeneratorDatas, this.Visible, this.CancellationToken))
                        {
                            monitor.InitializeComponent(this.Core);
                            await this.WithSubTask(monitor,
                                () => monitor.Create()
                            ).ConfigureAwait(false);
                        }
                    }
                    Logger.Write(this, LogLevel.Debug, "Completed successfully.");
                }
                else
                {
                    Logger.Write(this, LogLevel.Debug, "Nothing to do.");
                }
            }

            public override void Cancel()
            {
                if (this.MoodBar != null)
                {
                    this.MoodBar.Cancel();
                }
                base.Cancel();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MoodBarRenderInfo
    {
        public int Tint;

        public BitmapHelper.RenderInfo Background;
    }

    public static class DefaultColors
    {
        public static Color[] GetBackground()
        {
            return new[]
            {
                global::System.Windows.Media.Colors.Black
            };
        }
    }
}
