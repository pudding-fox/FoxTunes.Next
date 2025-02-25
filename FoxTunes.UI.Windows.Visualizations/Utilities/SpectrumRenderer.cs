using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FoxTunes
{
    public class SpectrumRenderer : VisualizationBase
    {
        const int MARGIN_MIN = 4;

        const int MARGIN_ZERO = 0;

        const int MARGIN_ONE = 1;

        public SpectrumRendererData RendererData { get; private set; }

        public SelectionConfigurationElement Bars { get; private set; }

        public BooleanConfigurationElement ShowPeaks { get; private set; }

        public IntegerConfigurationElement HoldInterval { get; private set; }

        public TextConfigurationElement ColorPalette { get; private set; }

        public IntegerConfigurationElement CutOff { get; private set; }

        public IntegerConfigurationElement PreAmp { get; private set; }

        public SelectionConfigurationElement FFTSize { get; private set; }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Bars = this.Configuration.GetElement<SelectionConfigurationElement>(
                   SpectrumConfiguration.SECTION,
                   SpectrumConfiguration.BARS_ELEMENT
                );
                this.ShowPeaks = this.Configuration.GetElement<BooleanConfigurationElement>(
                    SpectrumConfiguration.SECTION,
                    SpectrumConfiguration.PEAKS_ELEMENT
                 );
                this.HoldInterval = this.Configuration.GetElement<IntegerConfigurationElement>(
                   SpectrumConfiguration.SECTION,
                   SpectrumConfiguration.HOLD_ELEMENT
                );
                this.ColorPalette = this.Configuration.GetElement<TextConfigurationElement>(
                    SpectrumConfiguration.SECTION,
                    SpectrumConfiguration.COLOR_PALETTE_ELEMENT
                );
                this.CutOff = this.Configuration.GetElement<IntegerConfigurationElement>(
                    SpectrumConfiguration.SECTION,
                    SpectrumConfiguration.CUT_OFF_ELEMENT
                );
                this.PreAmp = this.Configuration.GetElement<IntegerConfigurationElement>(
                    SpectrumConfiguration.SECTION,
                    SpectrumConfiguration.PRE_AMP_ELEMENT
                );
                this.FFTSize = this.Configuration.GetElement<SelectionConfigurationElement>(
                   SpectrumConfiguration.SECTION,
                   VisualizationBehaviourConfiguration.FFT_SIZE_ELEMENT
                );
                this.Bars.ValueChanged += this.OnValueChanged;
                this.ShowPeaks.ValueChanged += this.OnValueChanged;
                this.ColorPalette.ValueChanged += this.OnValueChanged;
                this.CutOff.ValueChanged += this.OnValueChanged;
                this.PreAmp.ValueChanged += this.OnValueChanged;
                this.FFTSize.ValueChanged += this.OnValueChanged;
                var task = this.CreateBitmap();
            }
            base.OnConfigurationChanged();
        }

        protected virtual async void OnValueChanged(object sender, EventArgs e)
        {
            if (object.ReferenceEquals(sender, this.Bars))
            {
                //Changing bars requires full refresh.
                if (await this.CreateBitmap().ConfigureAwait(false))
                {
                    return;
                }
            }
            await this.CreateData().ConfigureAwait(false);
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
                SpectrumConfiguration.GetBars(this.Bars.Value),
                SpectrumConfiguration.GetFFTSize(this.Bars.Value, this.FFTSize.Value),
                this.ShowPeaks.Value,
                this.GetColorPalettes(this.GetColorPaletteOrDefault(this.ColorPalette.Value), this.ShowPeaks.Value),
                this.CutOff.Value,
                1.0f + FromDecibel(this.PreAmp.Value)
            );
            return true;
        }

        protected virtual IDictionary<string, IntPtr> GetColorPalettes(string value, bool showPeak)
        {
            var palettes = SpectrumConfiguration.GetColorPalette(value);
            var background = palettes.GetOrAdd(
                SpectrumConfiguration.COLOR_PALETTE_BACKGROUND,
                () => DefaultColors.GetBackground()
            );
            //Switch the default colors to the VALUE palette if one was provided.
            var colors = palettes.GetOrAdd(
                SpectrumConfiguration.COLOR_PALETTE_VALUE,
                () => DefaultColors.GetValue(new[] { this.ForegroundColor })
            );
            if (showPeak)
            {
                palettes.GetOrAdd(
                    SpectrumConfiguration.COLOR_PALETTE_PEAK,
                    () => DefaultColors.GetPeak(colors)
                );
            }
            return palettes.ToDictionary(
                pair => pair.Key,
                pair =>
                {
                    var flags = 0;
                    if (pair.Value.Length > 1)
                    {
                        flags |= BitmapHelper.COLOR_FROM_Y;
                    }
                    return BitmapHelper.CreatePalette(flags, GetAlphaBlending(pair.Key, pair.Value), pair.Value);
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
                    info = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[SpectrumConfiguration.COLOR_PALETTE_BACKGROUND]);
                }
                else
                {
                    var palettes = this.GetColorPalettes(this.GetColorPaletteOrDefault(this.ColorPalette.Value), false);
                    info = BitmapHelper.CreateRenderInfo(bitmap, palettes[SpectrumConfiguration.COLOR_PALETTE_BACKGROUND]);
                }
                BitmapHelper.DrawRectangle(ref info, 0, 0, data.Width, data.Height);
                bitmap.AddDirtyRect(new global::System.Windows.Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        protected virtual Task Render(SpectrumRendererData data)
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
                    Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render spectrum: {0}", e.Message);
#else
                    Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render spectrum, disabling: {0}", e.Message);
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
                UpdateValues(data);
                if (!data.LastUpdated.Equals(default(DateTime)) && data.Height >= 150)
                {
                    UpdateElementsSmooth(data.Values, data.ValueElements, data.Width, data.Height, data.Margin, Orientation.Vertical);
                }
                else
                {
                    UpdateElementsFast(data.Values, data.ValueElements, data.Width, data.Height, data.Margin, Orientation.Vertical);
                }
                if (data.Peaks != null)
                {
                    UpdatePeaks(data, this.UpdateInterval, this.HoldInterval.Value);
                }
                data.LastUpdated = DateTime.UtcNow;

                this.BeginUpdateData();
            }
            catch (Exception exception)
            {
#if DEBUG
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to update spectrum data: {0}", exception.Message);
                this.BeginUpdateData();
#else
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to update spectrum data, disabling: {0}", exception.Message);
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
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render spectrum data: {0}", exception.Message);
                this.BeginUpdateData();
#else
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render spectrum data, disabling: {0}", exception.Message);
#endif
            }
        }

        protected override int GetPixelWidth(double width)
        {
            if (this.Bars == null)
            {
                return 0;
            }
            var min = SpectrumConfiguration.GetWidth(this.Bars.Value);
            //TODO: Side effect from getter.
            this.MinWidth = min;
            var bars = SpectrumConfiguration.GetBars(this.Bars.Value);
            return base.GetPixelWidth(Math.Max(bars * (Convert.ToInt32(width) / bars), min));
        }

        protected override void OnDisposing()
        {
            if (this.Bars != null)
            {
                this.Bars.ValueChanged -= this.OnValueChanged;
            }
            if (this.ShowPeaks != null)
            {
                this.ShowPeaks.ValueChanged -= this.OnValueChanged;
            }
            if (this.ColorPalette != null)
            {
                this.ColorPalette.ValueChanged -= this.OnValueChanged;
            }
            if (this.CutOff != null)
            {
                this.CutOff.ValueChanged -= this.OnValueChanged;
            }
            if (this.PreAmp != null)
            {
                this.PreAmp.ValueChanged -= this.OnValueChanged;
            }
            if (this.FFTSize != null)
            {
                this.FFTSize.ValueChanged -= this.OnValueChanged;
            }
            base.OnDisposing();
        }

        private static SpectrumRenderInfo GetRenderInfo(WriteableBitmap bitmap, SpectrumRendererData data)
        {
            var info = new SpectrumRenderInfo()
            {
                Background = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[SpectrumConfiguration.COLOR_PALETTE_BACKGROUND])
            };
            if (data.PeakElements != null)
            {
                info.Peak = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[SpectrumConfiguration.COLOR_PALETTE_PEAK]);
            }
            if (data.ValueElements != null)
            {
                info.Value = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[SpectrumConfiguration.COLOR_PALETTE_VALUE]);
            }
            return info;
        }

        private static void Render(ref SpectrumRenderInfo info, SpectrumRendererData data)
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

        private static void UpdateValues(SpectrumRendererData data)
        {
            var samples = data.Samples;
            var values = data.Values;

            var samplesPerValue = data.FFTRange / data.Count;

            if (samplesPerValue == 1)
            {
                for (int a = 0; a < data.FFTRange && a < values.Length; a++)
                {
                    var value = samples[a];
                    values[a] = ToDecibelFixed(value);
                }
            }
            else if (samplesPerValue > 1)
            {
                for (int a = 0, b = 0; a < data.FFTRange && b < values.Length; a += samplesPerValue, b++)
                {
                    var value = default(float);
                    for (var c = 0; c < samplesPerValue; c++)
                    {
                        value += samples[a + c];
                    }
                    values[b] = ToDecibelFixed(value / samplesPerValue);
                }
            }
            else
            {
                var valuesPerSample = (float)data.Count / (data.FFTRange - 1);
                for (var a = 0; a < data.Count; a++)
                {
                    var value = samples[Convert.ToInt32(a / valuesPerSample)];
                    values[a] = ToDecibelFixed(value);
                }
            }

            if (data.PreAmp > 0)
            {
                for (var a = 0; a < data.Count; a++)
                {
                    values[a] = Math.Min(values[a] * data.PreAmp, 1.0f);
                }
            }
        }

        private static void UpdatePeaks(SpectrumRendererData data, int updateInterval, int holdInterval)
        {
            for (var a = 0; a < data.Peaks.Length; a++)
            {
                data.Peaks[a] = data.ValueElements[a].Y;
            }
            var duration = Convert.ToInt32(
                Math.Min(
                    (DateTime.UtcNow - data.LastUpdated).TotalMilliseconds,
                    updateInterval * 100
                )
            );
            UpdateElementsSmooth(data.Peaks, data.PeakElements, data.Holds, data.Width, data.Height, data.Margin, holdInterval, duration, Orientation.Vertical);
        }

        public static SpectrumRendererData Create(int width, int height, int count, int fftSize, bool showPeaks, IDictionary<string, IntPtr> colors, int cutOff, float preAmp)
        {
            if (count > width)
            {
                //Not enough space.
                return null;
            }

            var margin = width > (count * MARGIN_MIN) ? MARGIN_ONE : MARGIN_ZERO;
            var data = new SpectrumRendererData()
            {
                Width = width,
                Height = height,
                Margin = margin,
                Count = count,
                FFTSize = fftSize,
                Values = new float[count],
                Colors = colors,
                CutOff = cutOff,
                PreAmp = preAmp,
                ValueElements = new Int32Rect[count]
            };
            if (showPeaks)
            {
                data.Peaks = new int[count];
                data.Holds = new int[count];
                data.PeakElements = CreatePeaks(count);
            }
            return data;
        }

        private static Int32Rect[] CreatePeaks(int count)
        {
            var peaks = new Int32Rect[count];
            for (var a = 0; a < count; a++)
            {
                peaks[a].Y = int.MaxValue;
            }
            return peaks;
        }

        public class SpectrumRendererData : FFTVisualizationData
        {
            public int FFTRange;

            public float[] Values;

            public int Width;

            public int Height;

            public int Margin;

            public int Count;

            public IDictionary<string, IntPtr> Colors;

            public int CutOff;

            public float PreAmp;

            public Int32Rect[] ValueElements;

            public Int32Rect[] PeakElements;

            public int[] Peaks;

            public int[] Holds;

            public DateTime LastUpdated;

            public override void OnAllocated()
            {
                if (this.CutOff > 0)
                {
                    this.FFTRange = FrequencyToIndex(this.CutOff * 1000, this.FFTSize, this.Rate);
                }
                else
                {
                    this.FFTRange = this.Samples.Length;
                }
                base.OnAllocated();
            }

            ~SpectrumRendererData()
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
        public struct SpectrumRenderInfo
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
