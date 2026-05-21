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

        public IntegerConfigurationElement Resolution { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.Factory = ComponentRegistry.Instance.GetComponent<MoodBarFactory>();
            this.Cache = ComponentRegistry.Instance.GetComponent<MoodBarCache>();
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.Configuration = core.Components.Configuration;
            this.Resolution = this.Configuration.GetElement<IntegerConfigurationElement>(
                MoodBarGeneratorConfiguration.SECTION,
                MoodBarGeneratorConfiguration.RESOLUTION
            );
            this.Resolution.ValueChanged += this.OnValueChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            var task = this.Update(this.FileData);
        }

        protected virtual async Task Update(IFileData fileData)
        {
            if (this.GeneratorData != null)
            {
                this.GeneratorData.Updated -= this.OnUpdated;
                if (this.GeneratorData.CancellationToken != null)
                {
                    this.GeneratorData.CancellationToken.Cancel();
                }
            }
            if (fileData != null)
            {
                var generatorData = this.Cache.Get(fileData, this.Resolution.Value);
                if (generatorData != null)
                {
                    this.GeneratorData = generatorData;
                    await this.RefreshRendererTarget().ConfigureAwait(false);
                }
                else
                {
                    generatorData = new MoodBarGenerator.MoodBarGeneratorData()
                    {
                        FileName = fileData.FileName,
                        Resolution = this.Resolution.Value,
                        CancellationToken = new CancellationToken(),
                    };
                    this.GeneratorData = generatorData;
                    this.GeneratorData.Updated += this.OnUpdated;
                    await this.RefreshRendererTarget().ConfigureAwait(false);
                    using (var task = new CreateTask(this, new[] { fileData }, new[] { this.GeneratorData }))
                    {
                        task.InitializeComponent(this.Core);
                        await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                        await task.Run().ConfigureAwait(false);
                        if (task.IsFaulted)
                        {
                            return;
                        }
                    }
                    this.Cache.Save(fileData, generatorData);
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

        public Task Render(MoodBarRendererData data)
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
                    return;
                }
                var info = GetRenderInfo(target, data);
                Monitor.Enter(this.SyncRoot);
                try
                {
                    Render(ref info, data);
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
            Monitor.Enter(this.SyncRoot);
            try
            {
                var generatorData = this.GeneratorData;
                var rendererData = this.RendererData;
                if (generatorData != null && rendererData != null)
                {
                    try
                    {
                        Update(
                            generatorData,
                            rendererData
                        );
                    }
                    catch (Exception e)
                    {
                        Logger.Write(this.GetType(), LogLevel.Warn, "Failed to update mood bar data: {0}", e.Message);
                        return;
                    }
                    var task = this.Render(rendererData);
                }
            }
            finally
            {
                Monitor.Exit(this.SyncRoot);
            }
        }

        protected override int GetPixelWidth(double width)
        {
            var data = this.GeneratorData;
            if (data != null)
            {
                if (data.Capacity > 0)
                {
                    var valuesPerElement = Convert.ToInt32(
                        Math.Ceiling(
                            Math.Max(
                                (float)data.Capacity / width,
                                1
                            )
                        )
                    );
                    width = data.Capacity / valuesPerElement;
                }
            }
            return base.GetPixelWidth(width);
        }

        protected override void OnDisposing()
        {
            if (this.GeneratorData != null)
            {
                this.GeneratorData.Updated -= this.OnUpdated;
            }
            if (this.Resolution != null)
            {
                this.Resolution.ValueChanged -= this.OnValueChanged;
            }
            base.OnDisposing();
        }

        private static void Update(MoodBarGenerator.MoodBarGeneratorData generatorData, MoodBarRendererData rendererData)
        {
            UpdateView(generatorData, rendererData);
        }

        private static void UpdateView(MoodBarGenerator.MoodBarGeneratorData generatorData, MoodBarRendererData rendererData)
        {
            var valuesPerElement = rendererData.ValuesPerElement;

            for (; rendererData.View.Position < rendererData.Width; rendererData.View.Position++)
            {
                var valuePosition = rendererData.View.Position * rendererData.ValuesPerElement;
                if ((valuePosition + rendererData.ValuesPerElement) > generatorData.Position)
                {
                    break;
                }

                var low = default(float);
                var mid = default(float);
                var high = default(float);
                for (var a = 0; a < valuesPerElement; a++)
                {
                    low += generatorData.Data[valuePosition + a].Low;
                    mid += generatorData.Data[valuePosition + a].Mid;
                    high += generatorData.Data[valuePosition + a].High;
                }

                low /= valuesPerElement;
                mid /= valuesPerElement;
                high /= valuesPerElement;

                low = Math.Min(low, 1);
                mid = Math.Min(mid, 1);
                high = Math.Min(high, 1);

                rendererData.View.Peak = Math.Max(
                    Math.Max(
                        Math.Max(
                            low,
                            mid
                        ),
                        high
                    ),
                    rendererData.View.Peak
                );
                rendererData.View.Low[rendererData.View.Position] = low;
                rendererData.View.Mid[rendererData.View.Position] = mid;
                rendererData.View.High[rendererData.View.Position] = high;
            }
        }

        private static MoodBarRenderInfo GetRenderInfo(RendererTarget target, MoodBarRendererData data)
        {
            var info = new MoodBarRenderInfo()
            {
                Background = BitmapHelper.CreateRenderInfo(target, data.Colors[MoodBarStreamPositionConfiguration.COLOR_PALETTE_BACKGROUND])
            };
            return info;
        }

        public static void Render(ref MoodBarRenderInfo info, MoodBarRendererData data)
        {
            if (info.Background.Width != data.Width || info.Background.Height != data.Height)
            {
                //Bitmap does not match data.
                return;
            }
            BitmapHelper.DrawRectangle(ref info.Background, 0, 0, data.Width, data.Height);

            if (data.View.Position == 0)
            {
                //No data.
                return;
            }

            for (var a = 0; a < data.View.Position; a++)
            {
                var palette = default(IntPtr);
                {
                    var hue = ((data.View.Low[a] * 220f) + (data.View.Mid[a] * 120f) + (data.View.High[a] * 20f)) % 360f;
                    var saturation =
                        0.65f + (data.View.High[a] * 0.35f);
                    var value =
                        0.2f + (data.View.Mid[a] * 0.8f);
                    palette = BitmapHelper.CreatePalette(new[]
                    {
                       HsvToRgb(hue, saturation, value)
                    }, 1, 0);
                }
                try
                {
                    var value = BitmapHelper.CreateRenderInfo(info.Background, palette);
                    BitmapHelper.DrawRectangle(ref value, a, 0, 1, data.Height);
                }
                finally
                {
                    BitmapHelper.DestroyPalette(ref palette);
                }
            }
        }

        public static MoodBarRendererData Create(MoodBarGenerator.MoodBarGeneratorData generatorData, int width, int height, IDictionary<string, IntPtr> colors)
        {
            var valuesPerElement = generatorData.Capacity / width;
            if (valuesPerElement == 0)
            {
                valuesPerElement = 1;
            }
            var data = new MoodBarRendererData()
            {
                Width = width,
                Height = height,
                ValuesPerElement = valuesPerElement,
                Colors = colors,
                View = new MoodBarGeneratorDataView()
                {
                    Low = new float[width],
                    Mid = new float[width],
                    High = new float[width]
                }
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

        public class MoodBarGeneratorDataView
        {
            public float[] Low;

            public float[] Mid;

            public float[] High;

            public float Peak;

            public int Position;
        }

        public class MoodBarRendererData
        {
            public int Width;

            public int Height;

            public int ValuesPerElement;

            public IDictionary<string, IntPtr> Colors;

            public MoodBarGeneratorDataView View;

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
            public const string ID = "E42334F5-1BE8-421D-A679-060072868AC2";

            public static readonly IMoodBarFactory MoodBarFactory = ComponentRegistry.Instance.GetComponent<IMoodBarFactory>();

            private CreateTask() : base(ID)
            {
            }

            public CreateTask(MoodBarRendererBase renderer, IFileData[] fileDatas, IEnumerable<MoodBarGenerator.MoodBarGeneratorData> generatorDatas) : this()
            {
                this.Renderer = renderer;
                this.FileDatas = fileDatas;
                this.MoodBarItems = fileDatas
                    .OrderBy(fileData => fileData.FileName)
                    .Select(fileData => MoodBarItem.FromFileData(fileData))
                    .ToArray();
                this.GeneratorDatas = generatorDatas.ToArray();
            }

            public MoodBarRendererBase Renderer { get; private set; }

            public IFileData[] FileDatas { get; private set; }

            public MoodBarItem[] MoodBarItems { get; private set; }

            public MoodBarGenerator.MoodBarGeneratorData[] GeneratorDatas { get; private set; }

            public ICore Core { get; private set; }

            public ISignalEmitter SignalEmitter { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.Core = core;
                this.SignalEmitter = core.Components.SignalEmitter;
                base.InitializeComponent(core);
            }

            protected override async Task OnRun()
            {
                Logger.Write(this, LogLevel.Debug, "Creating.");
                using (var moodBar = MoodBarFactory.CreateMoodBar(this.MoodBarItems))
                {
                    Logger.Write(this, LogLevel.Debug, "Starting.");
                    using (var monitor = new MoodBarMonitor(moodBar, this.GeneratorDatas, this.Visible, this.CancellationToken))
                    {
                        await this.WithSubTask(monitor,
                            () => monitor.Create()
                        ).ConfigureAwait(false);
                    }
                }
                Logger.Write(this, LogLevel.Debug, "Completed successfully.");
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MoodBarRenderInfo
    {
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
