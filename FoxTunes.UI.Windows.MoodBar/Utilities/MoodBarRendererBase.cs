using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FoxTunes
{
    public class MoodBarRendererBase : RendererBase
    {
        const int CACHE_SIZE = 1024;

        const int TIMEOUT = 1000;

        public static Debouncer<IFileData, MoodBarGenerator.MoodBarGeneratorData> Debouncer { get; private set; }

        public static TaskScheduler Scheduler { get; private set; }

        public static CappedDictionary<string, MoodBarGenerator.MoodBarGeneratorData> Store { get; private set; }

        static MoodBarRendererBase()
        {
            Debouncer = new Debouncer<IFileData, MoodBarGenerator.MoodBarGeneratorData>(TIMEOUT);
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
            var task = this.Update(this.FileData);
        }

        public Task UpdateTask { get; private set; }

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
                        Debouncer.Exec((arg1, arg2) =>
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
            if (info.Background.Width != rendererData.Width || info.Background.Height != rendererData.Height || generatorData.Data == null)
            {
                return;
            }

            BitmapHelper.DrawRectangle(
                ref info.Background, 
                0, 
                0, 
                rendererData.Width, 
                rendererData.Height
            );

            var values = new float[generatorData.Data.GetLength(1)];
            var averages = new float[generatorData.Data.GetLength(1)];
            for (var a = 0; a < generatorData.Data.GetLength(0); a++)
            {
                for (var b = 0; b < generatorData.Data.GetLength(1); b++)
                {
                    averages[b] += generatorData.Data[a, b];
                }
            }

            for (var a = 0; a < generatorData.Data.GetLength(1); a++)
            {
                averages[a] /= generatorData.Data.GetLength(0);
                if (averages[a] <= 0)
                {
                    averages[a] = 1;
                }
            }

            for (var a = 0; a < generatorData.Data.GetLength(0); a++)
            {
                var b = (int)((a / (float)generatorData.Data.GetLength(0)) * rendererData.Width);
                var c = (int)(((a + 1) / (float)generatorData.Data.GetLength(0)) * rendererData.Width);
                var width = Math.Max(c - b, 1);
                if (b >= rendererData.Width)
                {
                    continue;
                }
                if (b + width > rendererData.Width)
                {
                    width = rendererData.Width - b;
                }
                for (var d = 0; d < generatorData.Data.GetLength(1); d++)
                {
                    var value = default(float);

                    if (a > 0 && a < generatorData.Data.GetLength(0) - 1)
                    {
                        value = (generatorData.Data[a - 1, d] * 0.25f) + (generatorData.Data[a, d] * 0.50f) + (generatorData.Data[a + 1, d] * 0.25f);
                    }
                    else
                    {
                        value = generatorData.Data[a, d];
                    }
                    value /= (float)Math.Pow(averages[d], 0.12);
                    if (value > 1.5f)
                    {
                        value = 1.5f;
                    }
                    values[d] = value;
                }
                {
                    var color = MoodBarColorProvider.GetColor(values, info.Tint);
                    var palette = BitmapHelper.CreatePalette(new[] { new Int32Color(color) }, 1, 0);
                    try
                    {
                        var render = BitmapHelper.CreateRenderInfo(info.Background, palette);
                        BitmapHelper.DrawRectangle(ref render, b, 0, width, rendererData.Height);
                    }
                    finally
                    {
                        BitmapHelper.DestroyPalette(ref palette);
                    }
                }
                {
                    const byte SHADE = 50;
                    var contrast = Color.FromRgb(SHADE, SHADE, SHADE);
                    var color = MoodBarColorProvider.GetColor(values, info.Tint).Shade(contrast);
                    var palette = BitmapHelper.CreatePalette(new[] { new Int32Color(color) }, 1, 0);
                    try
                    {
                        var value = values.Max();
                        var y = Convert.ToInt32((rendererData.Height / 2) - (value * (rendererData.Height / 2)));
                        var height = Math.Max(Convert.ToInt32((((rendererData.Height / 2) - y) + (value * (rendererData.Height / 2)))), 1);
                        var render = BitmapHelper.CreateRenderInfo(info.Background, palette);
                        BitmapHelper.DrawRectangle(ref render, b, y, width, height);
                    }
                    finally
                    {
                        BitmapHelper.DestroyPalette(ref palette);
                    }
                }
            }
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
