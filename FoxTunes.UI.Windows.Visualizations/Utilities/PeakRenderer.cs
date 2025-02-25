using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FoxTunes
{
    public class PeakRenderer : VisualizationBase
    {
        const int MARGIN = 1;

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation",
            typeof(Orientation),
            typeof(PeakRenderer),
            new FrameworkPropertyMetadata(Orientation.Horizontal, new PropertyChangedCallback(OnOrientationChanged))
        );

        public static Orientation GetOrientation(PeakRenderer source)
        {
            return (Orientation)source.GetValue(OrientationProperty);
        }

        public static void SetOrientation(PeakRenderer source, Orientation value)
        {
            source.SetValue(OrientationProperty, value);
        }

        public static void OnOrientationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var renderer = sender as PeakRenderer;
            if (renderer == null)
            {
                return;
            }
            renderer.OnOrientationChanged();
        }

        public PeakRendererData RendererData { get; private set; }

        public BooleanConfigurationElement ShowPeaks { get; private set; }

        public IntegerConfigurationElement HoldInterval { get; private set; }

        public TextConfigurationElement ColorPalette { get; private set; }

        public IntegerConfigurationElement MinFrequency { get; private set; }

        public IntegerConfigurationElement MaxFrequency { get; private set; }

        public IntegerConfigurationElement Duration { get; private set; }

        public SelectionConfigurationElement FFTSize { get; private set; }

        public Orientation Orientation
        {
            get
            {
                return (Orientation)this.GetValue(OrientationProperty);
            }
            set
            {
                this.SetValue(OrientationProperty, value);
            }
        }

        protected virtual void OnOrientationChanged()
        {
            var data = this.RendererData;
            if (data != null)
            {
                data.Orientation = this.Orientation;
            }
            if (this.OrientationChanged != null)
            {
                this.OrientationChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Orientation");
        }

        public event EventHandler OrientationChanged;

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.ShowPeaks = this.Configuration.GetElement<BooleanConfigurationElement>(
                    PeakMeterConfiguration.SECTION,
                    PeakMeterConfiguration.PEAKS_ELEMENT
                 );
                this.HoldInterval = this.Configuration.GetElement<IntegerConfigurationElement>(
                   PeakMeterConfiguration.SECTION,
                   PeakMeterConfiguration.HOLD_ELEMENT
                );
                this.ColorPalette = this.Configuration.GetElement<TextConfigurationElement>(
                    PeakMeterConfiguration.SECTION,
                    PeakMeterConfiguration.COLOR_PALETTE
                );
                this.MinFrequency = this.Configuration.GetElement<IntegerConfigurationElement>(
                    PeakMeterConfiguration.SECTION,
                    PeakMeterConfiguration.MIN_FREQUENCY
                );
                this.MaxFrequency = this.Configuration.GetElement<IntegerConfigurationElement>(
                    PeakMeterConfiguration.SECTION,
                    PeakMeterConfiguration.MAX_FREQUENCY
                );
                this.Duration = this.Configuration.GetElement<IntegerConfigurationElement>(
                    PeakMeterConfiguration.SECTION,
                    PeakMeterConfiguration.DURATION
                );
                this.FFTSize = this.Configuration.GetElement<SelectionConfigurationElement>(
                   EnhancedSpectrumConfiguration.SECTION,
                   VisualizationBehaviourConfiguration.FFT_SIZE_ELEMENT
                );
                this.ShowPeaks.ValueChanged += this.OnValueChanged;
                this.ColorPalette.ValueChanged += this.OnValueChanged;
                this.Duration.ValueChanged += this.OnValueChanged;
                this.FFTSize.ValueChanged += this.OnValueChanged;
                var task = this.CreateBitmap();
            }
            base.OnConfigurationChanged();
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            var task = this.CreateData();
        }

        protected override bool CreateData(int width, int height)
        {
            if (this.Configuration == null)
            {
                return false;
            }
            this.RendererData = Create(
                width,
                height,
                VisualizationBehaviourConfiguration.GetFFTSize(this.FFTSize.Value),
                this.ShowPeaks.Value,
                this.Duration.Value,
                this.GetColorPalettes(this.GetColorPaletteOrDefault(this.ColorPalette.Value), this.Orientation),
                this.Orientation
            );
            return true;
        }

        protected virtual IDictionary<string, IntPtr> GetColorPalettes(string value, Orientation orientation)
        {
            var flags = default(int);
            var palettes = PeakMeterConfiguration.GetColorPalette(value);
            var background = palettes.GetOrAdd(
                PeakMeterConfiguration.COLOR_PALETTE_BACKGROUND,
                () => DefaultColors.GetBackground()
            );
            //Switch the default colors to the VALUE palette if one was provided.
            var colors = palettes.GetOrAdd(
                PeakMeterConfiguration.COLOR_PALETTE_VALUE,
                () => DefaultColors.GetValue(new[] { this.ForegroundColor })
            );
            palettes.GetOrAdd(
                PeakMeterConfiguration.COLOR_PALETTE_PEAK,
                () => DefaultColors.GetPeak(colors)
            );
            return palettes.ToDictionary(
                pair => pair.Key,
                pair =>
                {
                    flags = 0;
                    colors = pair.Value;
                    if (colors.Length > 1)
                    {
                        if (orientation == Orientation.Horizontal)
                        {
                            flags |= BitmapHelper.COLOR_FROM_X;
                            colors = colors.Reverse().ToArray();
                        }
                        else if (orientation == Orientation.Vertical)
                        {
                            flags |= BitmapHelper.COLOR_FROM_Y;
                        }
                    }
                    return BitmapHelper.CreatePalette(flags, GetAlphaBlending(pair.Key, colors), colors);
                },
                StringComparer.OrdinalIgnoreCase
            );
        }

        protected override WriteableBitmap CreateBitmap(int width, int height)
        {
            var bitmap = base.CreateBitmap(width, height);
            this.ClearBitmap(bitmap);
            return bitmap;
        }

        protected override void ClearBitmap(WriteableBitmap bitmap)
        {
            if (!bitmap.TryLock(LockTimeout))
            {
                return;
            }
            try
            {
                var info = default(BitmapHelper.RenderInfo);
                var data = this.RendererData;
                if (data != null)
                {
                    info = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[PeakMeterConfiguration.COLOR_PALETTE_BACKGROUND]);
                }
                else
                {
                    var palettes = this.GetColorPalettes(this.GetColorPaletteOrDefault(this.ColorPalette.Value), this.Orientation);
                    info = BitmapHelper.CreateRenderInfo(bitmap, palettes[PeakMeterConfiguration.COLOR_PALETTE_BACKGROUND]);
                }
                BitmapHelper.DrawRectangle(ref info, 0, 0, data.Width, data.Height);
                bitmap.AddDirtyRect(new global::System.Windows.Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        protected virtual Task Render(PeakRendererData data)
        {
            return Windows.Invoke(() =>
            {
                var bitmap = this.Bitmap;
                if (bitmap == null)
                {
                    return;
                }

                if (!bitmap.TryLock(LockTimeout))
                {
                    return;
                }
                var success = default(bool);
                var info = GetRenderInfo(bitmap, data);
                try
                {
                    Render(ref info, data);
                    success = true;
                }
                catch (Exception e)
                {
#if DEBUG
                    Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render peaks: {0}", e.Message);
#else
                    Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render peaks, disabling: {0}", e.Message);
                    success = false;
#endif
                }
                bitmap.AddDirtyRect(new global::System.Windows.Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                bitmap.Unlock();
                if (!success)
                {
                    return;
                }
#if DEBUG
                if (this.ViewModel != null)
                {
                    Interlocked.Increment(ref this.ViewModel.Frames);
                }
#endif
            }, DISPATCHER_PRIORITY);
        }

        protected override void OnUpdateData(object sender, ElapsedEventArgs e)
        {
            var data = this.RendererData;
            if (data == null)
            {
                this.BeginUpdateData();
                return;
            }
            try
            {
                if (!this.VisualizationDataSource.Update(data))
                {
                    this.BeginUpdateData();
                    return;
                }
                UpdateValues(data, this.MinFrequency.Value, this.MaxFrequency.Value);
                if (!data.LastUpdated.Equals(default(DateTime)))
                {
                    UpdateElementsSmooth(data);
                }
                else
                {
                    UpdateElementsFast(data);
                }
                if (data.Peaks != null)
                {
                    UpdatePeaks(data, this.UpdateInterval, this.HoldInterval.Value, data.Orientation);
                }
                data.LastUpdated = DateTime.UtcNow;

                this.BeginUpdateData();
            }
            catch (Exception exception)
            {
#if DEBUG
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to update peak data: {0}", exception.Message);
                this.BeginUpdateData();
#else
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to update peak data, disabling: {0}", exception.Message);
#endif
            }
        }

        protected override async void OnUpdateDisplay(object sender, ElapsedEventArgs e)
        {
            var data = this.RendererData;
            if (data == null)
            {
                this.BeginUpdateDisplay();
                return;
            }
            try
            {
                await this.Render(data).ConfigureAwait(false);

                this.BeginUpdateDisplay();
            }
            catch (Exception exception)
            {
#if DEBUG
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render peak data: {0}", exception.Message);
                this.BeginUpdateData();
#else
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render peak data, disabling: {0}", exception.Message);
#endif
            }
        }

        protected override void OnDisposing()
        {
            if (this.ShowPeaks != null)
            {
                this.ShowPeaks.ValueChanged -= this.OnValueChanged;
            }
            if (this.ColorPalette != null)
            {
                this.ColorPalette.ValueChanged -= this.OnValueChanged;
            }
            if (this.Duration != null)
            {
                this.Duration.ValueChanged -= this.OnValueChanged;
            }
            base.OnDisposing();
        }

        private static PeakRenderInfo GetRenderInfo(WriteableBitmap bitmap, PeakRendererData data)
        {
            return new PeakRenderInfo()
            {
                Background = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[PeakMeterConfiguration.COLOR_PALETTE_BACKGROUND]),
                Value = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[PeakMeterConfiguration.COLOR_PALETTE_VALUE]),
                Peak = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[PeakMeterConfiguration.COLOR_PALETTE_PEAK])
            };
        }

        private static void Render(ref PeakRenderInfo info, PeakRendererData data)
        {
            if (info.Background.Width != data.Width || info.Background.Height != data.Height)
            {
                //Bitmap does not match data.
                return;
            }

            BitmapHelper.DrawRectangle(ref info.Background, 0, 0, data.Width, data.Height);

            if (data.SampleCount == 0)
            {
                //No data.
                return;
            }

            if (data.PeakElements != null)
            {
                BitmapHelper.DrawRectangles(ref info.Peak, data.PeakElements, data.PeakElements.Length);
            }
            if (data.ValueElements != null)
            {
                BitmapHelper.DrawRectangles(ref info.Value, data.ValueElements, data.ValueElements.Length);
            }
        }

        private static void UpdateValues(PeakRendererData data, int minFrequency, int maxFrequency)
        {
            var min = FrequencyToIndex(minFrequency, data.FFTSize, data.Rate);
            var max = FrequencyToIndex(maxFrequency, data.FFTSize, data.Rate);
            var count = max - min;
            if (count == 0)
            {
                return;
            }
            for (var channel = 0; channel < data.Channels; channel++)
            {
                data.Values[channel] = 0;
                for (var a = min; a < max; a++)
                {
                    data.Values[channel] += data.History.Peak[channel, a];
                }
                data.Values[channel] = ToDecibelFixed(data.Values[channel] / count);
            }
        }

        private static void UpdatePeaks(PeakRendererData data, int updateInterval, int holdInterval, Orientation orientation)
        {
            for (var a = 0; a < data.Peaks.Length; a++)
            {
                switch (orientation)
                {
                    case Orientation.Horizontal:
                        data.Peaks[a] = data.ValueElements[a].Width;
                        break;
                    case Orientation.Vertical:
                        data.Peaks[a] = data.ValueElements[a].Y;
                        break;
                }
            }
            var duration = Convert.ToInt32(
                Math.Min(
                    (DateTime.UtcNow - data.LastUpdated).TotalMilliseconds,
                    updateInterval * 100
                )
            );
            UpdateElementsSmooth(data.Peaks, data.PeakElements, data.Holds, data.Width, data.Height, MARGIN, holdInterval, duration, data.Orientation);
        }

        private static void UpdateElementsFast(PeakRendererData data)
        {
            UpdateElementsFast(data.Values, data.ValueElements, data.Width, data.Height, MARGIN, data.Orientation);
        }

        private static void UpdateElementsSmooth(PeakRendererData data)
        {
            UpdateElementsSmooth(data.Values, data.ValueElements, data.Width, data.Height, MARGIN, data.Orientation);
        }

        public static PeakRendererData Create(int width, int height, int fftSize, bool showPeaks, int history, IDictionary<string, IntPtr> colors, Orientation orientation)
        {
            var data = new PeakRendererData(history)
            {
                Width = width,
                Height = height,
                FFTSize = fftSize,
                ShowPeaks = showPeaks,
                Colors = colors,
                Orientation = orientation,
                //TODO: This should be PeakRenderer.UpdateInterval I think.
                Interval = TimeSpan.FromMilliseconds(100),
                Flags = VisualizationDataFlags.Individual
            };
            return data;
        }

        private static Int32Rect[] CreatePeaks(int count)
        {
            var peaks = new Int32Rect[count];
            for (var a = 0; a < count; a++)
            {
                peaks[a].X = int.MinValue;
                peaks[a].Y = int.MaxValue;
            }
            return peaks;
        }

        public class PeakRendererData : FFTVisualizationData
        {
            public PeakRendererData(int history)
            {
                this.Flags = VisualizationDataFlags.Individual;
                this.History = new VisualizationDataHistory()
                {
                    Capacity = history,
                    Flags = VisualizationDataHistoryFlags.None
                };
                this.History.Flags |= VisualizationDataHistoryFlags.Peak;
            }

            public int Width;

            public int Height;

            public bool ShowPeaks;

            public IDictionary<string, IntPtr> Colors;

            public Orientation Orientation;

            public float[] Values;

            public Int32Rect[] ValueElements;

            public Int32Rect[] PeakElements;

            public int[] Peaks;

            public int[] Holds;

            public DateTime LastUpdated;

            public override void OnAllocated()
            {
                this.Values = new float[this.Channels];
                this.ValueElements = new Int32Rect[this.Channels];
                if (this.ShowPeaks)
                {
                    this.Peaks = new int[this.Channels];
                    this.Holds = new int[this.Channels];
                    this.PeakElements = CreatePeaks(this.Channels);
                }
                base.OnAllocated();
            }

            ~PeakRendererData()
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

        [StructLayout(LayoutKind.Sequential)]
        public struct PeakRenderInfo
        {
            public BitmapHelper.RenderInfo Peak;

            public BitmapHelper.RenderInfo Value;

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

            public static Color[] GetPeak(Color[] colors)
            {
                var color = colors.FirstOrDefault();
                return new[]
                {
                    color
                };
            }

            public static Color[] GetValue(Color[] colors)
            {
                return colors;
            }
        }
    }
}
