using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FoxTunes
{
    public class OscilloscopeRenderer : VisualizationBase
    {
        public OscilloscopeRendererData RendererData { get; private set; }

        public SelectionConfigurationElement Mode { get; private set; }

        public IntegerConfigurationElement Window { get; private set; }

        public TextConfigurationElement ColorPalette { get; private set; }

        public IntegerConfigurationElement Duration { get; private set; }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Mode = this.Configuration.GetElement<SelectionConfigurationElement>(
                    OscilloscopeConfiguration.SECTION,
                    OscilloscopeConfiguration.MODE_ELEMENT
                );
                this.Window = this.Configuration.GetElement<IntegerConfigurationElement>(
                    OscilloscopeConfiguration.SECTION,
                    OscilloscopeConfiguration.WINDOW_ELEMENT
                );
                this.ColorPalette = this.Configuration.GetElement<TextConfigurationElement>(
                    OscilloscopeConfiguration.SECTION,
                    OscilloscopeConfiguration.COLOR_PALETTE
                );
                this.Duration = this.Configuration.GetElement<IntegerConfigurationElement>(
                    OscilloscopeConfiguration.SECTION,
                    OscilloscopeConfiguration.DURATION_ELEMENT
                );
                this.Mode.ValueChanged += this.OnValueChanged;
                this.Window.ValueChanged += this.OnValueChanged;
                this.ColorPalette.ValueChanged += this.OnValueChanged;
                this.Duration.ValueChanged += this.OnValueChanged;
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
                OscilloscopeConfiguration.GetWindow(this.Window.Value),
                OscilloscopeConfiguration.GetDuration(this.Duration.Value),
                this.GetColorPalettes(this.GetColorPaletteOrDefault(this.ColorPalette.Value)),
                OscilloscopeConfiguration.GetMode(this.Mode.Value)
            );
            return true;
        }

        protected virtual IDictionary<string, IntPtr> GetColorPalettes(string value)
        {
            var palettes = OscilloscopeConfiguration.GetColorPalette(value);
            var background = palettes.GetOrAdd(
                OscilloscopeConfiguration.COLOR_PALETTE_BACKGROUND,
                () => DefaultColors.GetBackground()
            );
            //Switch the default colors to the VALUE palette if one was provided.
            var colors = palettes.GetOrAdd(
                OscilloscopeConfiguration.COLOR_PALETTE_VALUE,
                () => DefaultColors.GetValue(new[] { this.ForegroundColor })
            );
            return palettes.ToDictionary(
                pair => pair.Key,
                pair =>
                {
                    if (pair.Value == null)
                    {
                        return IntPtr.Zero;
                    }
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
                    info = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[OscilloscopeConfiguration.COLOR_PALETTE_BACKGROUND]);
                }
                else
                {
                    var palettes = this.GetColorPalettes(this.GetColorPaletteOrDefault(this.ColorPalette.Value));
                    info = BitmapHelper.CreateRenderInfo(bitmap, palettes[OscilloscopeConfiguration.COLOR_PALETTE_BACKGROUND]);
                }
                BitmapHelper.DrawRectangle(ref info, 0, 0, data.Width, data.Height);
                bitmap.AddDirtyRect(new global::System.Windows.Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        protected virtual Task Render(OscilloscopeRendererData data)
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
                    Render(info, data);
                    success = true;
                }
                catch (Exception e)
                {
#if DEBUG
                    Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render oscilloscope: {0}", e.Message);
#else
                    Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render oscilloscope, disabling: {0}", e.Message);
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

                switch (data.Mode)
                {
                    default:
                    case OscilloscopeRendererMode.Mono:
                        if (!data.LastUpdated.Equals(default(DateTime)))
                        {
                            UpdateElementsSmoothMono(data.Values, data.Peaks, data.Elements, data.Width, data.Height);
                        }
                        else
                        {
                            UpdateElementsFastMono(data.Values, data.Peaks, data.Elements, data.Width, data.Height);
                        }
                        break;
                    case OscilloscopeRendererMode.Seperate:
                        if (!data.LastUpdated.Equals(default(DateTime)))
                        {
                            UpdateElementsSmoothSeperate(data.Values, data.Peaks, data.Elements, data.Width, data.Height, data.Channels);
                        }
                        else
                        {
                            UpdateElementsFastSeperate(data.Values, data.Peaks, data.Elements, data.Width, data.Height, data.Channels);
                        }
                        break;
                }

                this.BeginUpdateData();
            }
            catch (Exception exception)
            {
#if DEBUG
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to update oscilloscope data: {0}", exception.Message);
                this.BeginUpdateData();
#else
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to update oscilloscope data, disabling: {0}", exception.Message);
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
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render oscilloscope data: {0}", exception.Message);
                this.BeginUpdateData();
#else
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render oscilloscope data, disabling: {0}", exception.Message);
#endif
            }
        }

        private static void UpdateElementsFastMono(float[,] values, float[] peaks, Int32Point[,] elements, int width, int height)
        {
            if (values.Length == 0 || peaks[0] == 0.0f)
            {
                return;
            }
            height = height - 1;
            var center = height / 2;
            var peak = peaks[0];
            for (var x = 0; x < width; x++)
            {
                var y = default(int);
                var value = values[0, x];
                if (value < 0)
                {
                    y = center + Convert.ToInt32((Math.Abs(value) / peak) * (height / 2));
                }
                else if (value > 0)
                {
                    y = center - Convert.ToInt32((Math.Abs(value) / peak) * (height / 2));
                }
                else
                {
                    y = center;
                }
                elements[0, x].X = x;
                elements[0, x].Y = y;
            }
        }

        private static void UpdateElementsFastSeperate(float[,] values, float[] peaks, Int32Point[,] elements, int width, int height, int channels)
        {
            if (values.Length == 0 || channels == 0)
            {
                return;
            }
            height = height - 1;
            height = height / channels;
            for (var channel = 0; channel < channels; channel++)
            {
                var center = (height * channel) + (height / 2);
                var peak = peaks[channel];
                if (peak == 0.0f)
                {
                    continue;
                }
                for (var x = 0; x < width; x++)
                {
                    var y = default(int);
                    var value = values[channel, x];
                    if (value < 0)
                    {
                        y = center + Convert.ToInt32((Math.Abs(value) / peak) * (height / 2));
                    }
                    else if (value > 0)
                    {
                        y = center - Convert.ToInt32((Math.Abs(value) / peak) * (height / 2));
                    }
                    else
                    {
                        y = center;
                    }
                    elements[channel, x].X = x;
                    elements[channel, x].Y = y;
                }
            }
        }

        private static void UpdateElementsSmoothMono(float[,] values, float[] peaks, Int32Point[,] elements, int width, int height)
        {
            if (values.Length == 0 || peaks[0] == 0.0f)
            {
                return;
            }
            height = height - 1;
            height = height / 2;
            var center = height;
            var peak = peaks[0];
            var minChange = 1;
            var maxChange = height;
            for (var x = 0; x < width; x++)
            {
                var y = default(int);
                var value = values[0, x];
                if (value < 0)
                {
                    y = center + Convert.ToInt32((Math.Abs(value) / peak) * height);
                }
                else if (value > 0)
                {
                    y = center - Convert.ToInt32((Math.Abs(value) / peak) * height);
                }
                else
                {
                    y = center;
                }
                elements[0, x].X = x;
                Animate(ref elements[0, x].Y, y, center - height, center + height, minChange, maxChange);
            }
        }

        private static void UpdateElementsSmoothSeperate(float[,] values, float[] peaks, Int32Point[,] elements, int width, int height, int channels)
        {
            if (values.Length == 0 || channels == 0)
            {
                return;
            }
            height = height - 1;
            height = (height / channels) / 2;
            var minChange = 1;
            var maxChange = height;
            for (var channel = 0; channel < channels; channel++)
            {
                var center = ((height * 2) * channel) + height;
                var peak = peaks[channel];
                if (peak == 0.0f)
                {
                    continue;
                }
                for (var x = 0; x < width; x++)
                {
                    var y = default(int);
                    var value = values[channel, x];
                    if (value < 0)
                    {
                        y = center + Convert.ToInt32((Math.Abs(value) / peak) * height);
                    }
                    else if (value > 0)
                    {
                        y = center - Convert.ToInt32((Math.Abs(value) / peak) * height);
                    }
                    else
                    {
                        y = center;
                    }
                    elements[channel, x].X = x;
                    Animate(ref elements[channel, x].Y, y, center - height, center + height, minChange, maxChange);
                }
            }
        }

        protected override void OnDisposing()
        {
            if (this.Mode != null)
            {
                this.Mode.ValueChanged -= this.OnValueChanged;
            }
            if (this.Window != null)
            {
                this.Window.ValueChanged -= this.OnValueChanged;
            }
            if (this.Duration != null)
            {
                this.Duration.ValueChanged -= this.OnValueChanged;
            }
            base.OnDisposing();
        }

        private static OscilloscopeRenderInfo GetRenderInfo(WriteableBitmap bitmap, OscilloscopeRendererData data)
        {
            var info = new OscilloscopeRenderInfo()
            {
                Value = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[OscilloscopeConfiguration.COLOR_PALETTE_VALUE]),
                Background = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[OscilloscopeConfiguration.COLOR_PALETTE_BACKGROUND])
            };
            return info;
        }

        private static void Render(OscilloscopeRenderInfo info, OscilloscopeRendererData data)
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

            BitmapHelper.DrawLines(ref info.Value, data.Elements, data.Elements.GetLength(0), data.Elements.GetLength(1));
        }

        private static void UpdateValues(OscilloscopeRendererData data)
        {
            if (data.Width == 0 || data.Channels == 0 || data.SampleCount == 0)
            {
                return;
            }
            switch (data.Mode)
            {
                default:
                case OscilloscopeRendererMode.Mono:
                    UpdateValuesMono(data.History.Avg, data.Values, data.Peaks, data.Width, data.SampleCount);
                    break;
                case OscilloscopeRendererMode.Seperate:
                    UpdateValuesSeperate(data.History.Avg, data.Values, data.Peaks, data.Channels, data.Width, data.SampleCount);
                    break;
            }
            data.LastUpdated = DateTime.UtcNow;
        }

        private static void UpdateValuesMono(float[,] samples, float[,] values, float[] peaks, int width, int count)
        {
            var samplesPerValue = count / width;
            peaks[0] = 0.0f;
            if (samplesPerValue == 1)
            {
                for (int x = 0; x < width; x++)
                {
                    values[0, x] = samples[0, x];
                    peaks[0] = Math.Max(peaks[0], Math.Abs(values[0, x]));
                }
            }
            else if (samplesPerValue > 1)
            {
                for (int a = 0, x = 0; a < count && x < width; a += samplesPerValue, x++)
                {
                    values[0, x] = 0.0f;
                    for (var b = 0; b < samplesPerValue; b++)
                    {
                        values[0, x] += samples[0, a + b];
                    }
                    values[0, x] /= samplesPerValue;
                    peaks[0] = Math.Max(peaks[0], Math.Abs(values[0, x]));
                }
            }
            else
            {
                var valuesPerSample = (float)width / count;
                for (var x = 0; x < width; x++)
                {
                    values[0, x] = samples[0, Convert.ToInt32(x / valuesPerSample)];
                    peaks[0] = Math.Max(peaks[0], Math.Abs(values[0, x]));
                }
            }
        }

        private static void UpdateValuesSeperate(float[,] samples, float[,] values, float[] peaks, int channels, int width, int count)
        {
            var samplesPerValue = (count / channels) / width;
            if (samplesPerValue == 1)
            {
                for (var channel = 0; channel < channels; channel++)
                {
                    peaks[channel] = 0.0f;
                    for (var x = 0; x < width; x++)
                    {
                        values[channel, x] = samples[channel, x];
                        peaks[channel] = Math.Max(peaks[channel], Math.Abs(values[channel, x]));
                    }
                }
            }
            else if (samplesPerValue > 1)
            {
                for (var channel = 0; channel < channels; channel++)
                {
                    peaks[channel] = 0.0f;
                    for (int a = 0, x = 0; a < count && x < width; a += samplesPerValue, x++)
                    {
                        values[channel, x] = 0.0f;
                        for (var b = 0; b < samplesPerValue; b++)
                        {
                            values[channel, x] += samples[channel, a + b];
                        }
                        values[channel, x] /= samplesPerValue;
                        peaks[channel] = Math.Max(peaks[channel], Math.Abs(values[channel, x]));
                    }
                }
            }
            else
            {
                var valuesPerSample = (float)width / count;
                for (var x = 0; x < width; x++)
                {
                    for (var channel = 0; channel < channels; channel++)
                    {
                        values[channel, x] = samples[channel, Convert.ToInt32(x / valuesPerSample)];
                        peaks[channel] = Math.Max(peaks[channel], Math.Abs(values[channel, x]));
                    }
                }
            }
        }

        public static OscilloscopeRendererData Create(int width, int height, TimeSpan window, TimeSpan duration, IDictionary<string, IntPtr> colors, OscilloscopeRendererMode mode)
        {
            var data = new OscilloscopeRendererData()
            {
                Width = width,
                Height = height,
                Interval = window,
                Duration = duration,
                Colors = colors,
                Mode = mode,
                Flags = mode.HasFlag(OscilloscopeRendererMode.Seperate) ? VisualizationDataFlags.Individual : VisualizationDataFlags.None
            };
            return data;
        }

        public class OscilloscopeRendererData : PCMVisualizationData
        {
            public OscilloscopeRendererData()
            {
                this.History = new VisualizationDataHistory()
                {
                    Flags = VisualizationDataHistoryFlags.Average
                };
            }

            public int Width;

            public int Height;

            public float[,] Values;

            public float[] Peaks;

            public IDictionary<string, IntPtr> Colors;

            public Int32Point[,] Elements;

            public DateTime LastUpdated;

            public TimeSpan Duration;

            public OscilloscopeRendererMode Mode;

            public override void OnAllocated()
            {
                this.History.Capacity = GetHistoryCapacity(
                    Convert.ToInt32(this.Interval.TotalMilliseconds),
                    Convert.ToInt32(this.Duration.TotalMilliseconds)
                );

                //TODO: Only realloc if required.
                switch (this.Mode)
                {
                    default:
                    case OscilloscopeRendererMode.Mono:
                        this.Values = new float[1, this.Width];
                        this.Elements = CreateElements(1, this.Width, this.Height);
                        this.Peaks = new float[1];
                        break;
                    case OscilloscopeRendererMode.Seperate:
                        this.Values = new float[this.Channels, this.Width];
                        this.Elements = CreateElements(this.Channels, this.Width, this.Height);
                        this.Peaks = new float[this.Channels];
                        break;
                }
                base.OnAllocated();
            }

            ~OscilloscopeRendererData()
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

            private static Int32Point[,] CreateElements(int channels, int width, int height)
            {
                if (channels == 0)
                {
                    return null;
                }
                height = height / channels;
                var elements = new Int32Point[channels, width];
                for (var channel = 0; channel < channels; channel++)
                {
                    var center = (height * channel) + (height / 2);
                    for (var x = 0; x < width; x++)
                    {
                        elements[channel, x].X = x;
                        elements[channel, x].Y = center;
                    }
                }
                return elements;
            }

            private static int GetHistoryCapacity(int duration, int interval)
            {
                var capacity = Convert.ToInt32(Math.Ceiling((float)interval / duration));
                return capacity;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OscilloscopeRenderInfo
        {
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

            public static Color[] GetValue(Color[] colors)
            {
                return colors;
            }
        }
    }

    public enum OscilloscopeRendererMode : byte
    {
        None,
        Mono,
        Seperate
    }
}
