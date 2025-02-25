using FoxDb;
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
    public class EnhancedSpectrumRenderer : VisualizationBase
    {
        const int MARGIN_MIN = 4;

        const int MARGIN_ZERO = 0;

        const int MARGIN_ONE = 1;

        public IFFTDataTransformerFactory TransformerFactory { get; private set; }

        public SpectrumRendererData RendererData { get; private set; }

        public SelectionConfigurationElement Bands { get; private set; }

        public TextConfigurationElement Custom { get; private set; }

        public BooleanConfigurationElement ShowPeak { get; private set; }

        public BooleanConfigurationElement ShowRms { get; private set; }

        public TextConfigurationElement ColorPalette { get; private set; }

        public IntegerConfigurationElement Duration { get; private set; }

        public SelectionConfigurationElement FFTSize { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.TransformerFactory = core.Factories.FFTDataTransformer;
            base.InitializeComponent(core);
        }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Bands = this.Configuration.GetElement<SelectionConfigurationElement>(
                   EnhancedSpectrumConfiguration.SECTION,
                   EnhancedSpectrumConfiguration.BANDS_ELEMENT
               );
                this.Custom = this.Configuration.GetElement<TextConfigurationElement>(
                   EnhancedSpectrumConfiguration.SECTION,
                   EnhancedSpectrumConfiguration.BANDS_CUSTOM_ELEMENT
                );
                this.ShowPeak = this.Configuration.GetElement<BooleanConfigurationElement>(
                    EnhancedSpectrumConfiguration.SECTION,
                    EnhancedSpectrumConfiguration.PEAK_ELEMENT
                 );
                this.ShowRms = this.Configuration.GetElement<BooleanConfigurationElement>(
                    EnhancedSpectrumConfiguration.SECTION,
                    EnhancedSpectrumConfiguration.RMS_ELEMENT
                 );
                this.ColorPalette = this.Configuration.GetElement<TextConfigurationElement>(
                    EnhancedSpectrumConfiguration.SECTION,
                    EnhancedSpectrumConfiguration.COLOR_PALETTE_ELEMENT
                );
                this.Duration = this.Configuration.GetElement<IntegerConfigurationElement>(
                    EnhancedSpectrumConfiguration.SECTION,
                    EnhancedSpectrumConfiguration.DURATION_ELEMENT
                );
                this.FFTSize = this.Configuration.GetElement<SelectionConfigurationElement>(
                   EnhancedSpectrumConfiguration.SECTION,
                   VisualizationBehaviourConfiguration.FFT_SIZE_ELEMENT
                );
                this.Bands.ValueChanged += this.OnValueChanged;
                this.Custom.ValueChanged += this.OnValueChanged;
                this.ShowPeak.ValueChanged += this.OnValueChanged;
                this.ShowRms.ValueChanged += this.OnValueChanged;
                this.ColorPalette.ValueChanged += this.OnValueChanged;
                this.Duration.ValueChanged += this.OnValueChanged;
                this.FFTSize.ValueChanged += this.OnValueChanged;
                var task = this.CreateBitmap();
            }
            base.OnConfigurationChanged();
        }

        protected virtual async void OnValueChanged(object sender, EventArgs e)
        {
            if (object.ReferenceEquals(sender, this.Bands) || object.ReferenceEquals(sender, this.Custom))
            {
                //Changing bands requires full refresh.
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
            var bands = EnhancedSpectrumConfiguration.GetBands(this.Bands.Value, this.Custom);
            this.RendererData = Create(
                this.TransformerFactory.Create(bands),
                width,
                height,
                bands,
                VisualizationBehaviourConfiguration.GetFFTSize(this.FFTSize.Value),
                this.ShowPeak.Value,
                this.ShowRms.Value,
                this.Duration.Value,
                this.GetColorPalettes(this.GetColorPaletteOrDefault(this.ColorPalette.Value), this.ShowPeak.Value, this.ShowRms.Value)
            );
            return true;
        }

        protected virtual IDictionary<string, IntPtr> GetColorPalettes(string value, bool showPeak, bool showRms)
        {
            var palettes = EnhancedSpectrumConfiguration.GetColorPalette(value);
            var background = palettes.GetOrAdd(
                EnhancedSpectrumConfiguration.COLOR_PALETTE_BACKGROUND,
                () => DefaultColors.GetBackground()
            );
            //Switch the default colors to the VALUE palette if one was provided.
            var colors = palettes.GetOrAdd(
                EnhancedSpectrumConfiguration.COLOR_PALETTE_VALUE,
                () => DefaultColors.GetValue(new[] { this.ForegroundColor })
            );
            if (showPeak)
            {
                palettes.GetOrAdd(
                    EnhancedSpectrumConfiguration.COLOR_PALETTE_PEAK,
                    () => DefaultColors.GetPeak(background, showRms, colors)
                );
            }
            if (showRms)
            {
                palettes.GetOrAdd(
                    EnhancedSpectrumConfiguration.COLOR_PALETTE_RMS,
                    () => DefaultColors.GetRms(background, showPeak, colors)
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
                    info = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[EnhancedSpectrumConfiguration.COLOR_PALETTE_BACKGROUND]);
                }
                else
                {
                    var palettes = this.GetColorPalettes(this.GetColorPaletteOrDefault(this.ColorPalette.Value), false, false);
                    info = BitmapHelper.CreateRenderInfo(bitmap, palettes[EnhancedSpectrumConfiguration.COLOR_PALETTE_BACKGROUND]);
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
                else
                {
                    UpdateValues(data);
                    if (!data.LastUpdated.Equals(default(DateTime)) && data.Height >= 150)
                    {
                        UpdateElementsSmooth(data);
                    }
                    else
                    {
                        UpdateElementsFast(data);
                    }
                    data.LastUpdated = DateTime.UtcNow;
                }

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
            if (this.Bands == null)
            {
                return 0;
            }
            var bands = EnhancedSpectrumConfiguration.GetBands(this.Bands.Value, this.Custom);
            return base.GetPixelWidth(Math.Max(bands.Length * (Convert.ToInt32(width) / bands.Length), bands.Length));
        }

        protected override void OnDisposing()
        {
            if (this.Bands != null)
            {
                this.Bands.ValueChanged -= this.OnValueChanged;
            }
            if (this.Custom != null)
            {
                this.Custom.ValueChanged -= this.OnValueChanged;
            }
            if (this.ShowPeak != null)
            {
                this.ShowPeak.ValueChanged -= this.OnValueChanged;
            }
            if (this.ShowRms != null)
            {
                this.ShowRms.ValueChanged -= this.OnValueChanged;
            }
            if (this.ColorPalette != null)
            {
                this.ColorPalette.ValueChanged -= this.OnValueChanged;
            }
            if (this.Duration != null)
            {
                this.Duration.ValueChanged -= this.OnValueChanged;
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
                Background = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[EnhancedSpectrumConfiguration.COLOR_PALETTE_BACKGROUND])
            };
            if (data.PeakElements != null)
            {
                info.Peak = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[EnhancedSpectrumConfiguration.COLOR_PALETTE_PEAK]);
            }
            if (data.RmsElements != null)
            {
                info.Rms = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[EnhancedSpectrumConfiguration.COLOR_PALETTE_RMS]);
            }
            if (data.ValueElements != null)
            {
                info.Value = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[EnhancedSpectrumConfiguration.COLOR_PALETTE_VALUE]);
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
            if (data.RmsElements != null)
            {
                BitmapHelper.DrawRectangles(ref info.Rms, data.RmsElements, data.RmsElements.Length);
            }
            if (data.ValueElements != null)
            {
                BitmapHelper.DrawRectangles(ref info.Value, data.ValueElements, data.ValueElements.Length);
            }
        }

        private static void UpdateValues(SpectrumRendererData data)
        {
            var values = data.Values;
            var peakValues = data.PeakValues;
            var rmsValues = data.RmsValues;
            var doPeaks = peakValues != null;
            var doRms = rmsValues != null;
            data.Transformer.Transform(data, data.Values, data.PeakValues, data.RmsValues);
            for (var a = 0; a < values.Length; a++)
            {
                values[a] = ToDecibelFixed(values[a]);
                if (doPeaks)
                {
                    peakValues[a] = ToDecibelFixed(peakValues[a]);
                }
                if (doRms)
                {
                    rmsValues[a] = ToDecibelFixed(rmsValues[a]);
                }
            }
        }

        private static void UpdateElementsFast(SpectrumRendererData data)
        {
            UpdateElementsFast(data.Values, data.ValueElements, data.Width, data.Height, data.Margin, Orientation.Vertical);
            if (data.PeakValues != null && data.PeakElements != null)
            {
                UpdateElementsFast(data.PeakValues, data.PeakElements, data.Width, data.Height, data.Margin, Orientation.Vertical);
            }
            if (data.RmsValues != null && data.RmsElements != null)
            {
                UpdateElementsFast(data.RmsValues, data.RmsElements, data.Width, data.Height, data.Margin, Orientation.Vertical);
            }
        }

        private static void UpdateElementsSmooth(SpectrumRendererData data)
        {
            UpdateElementsSmooth(data.Values, data.ValueElements, data.Width, data.Height, data.Margin, Orientation.Vertical);
            if (data.PeakValues != null && data.PeakElements != null)
            {
                UpdateElementsSmooth(data.PeakValues, data.PeakElements, data.Width, data.Height, data.Margin, Orientation.Vertical);
            }
            if (data.RmsValues != null && data.RmsElements != null)
            {
                UpdateElementsSmooth(data.RmsValues, data.RmsElements, data.Width, data.Height, data.Margin, Orientation.Vertical);
            }
        }

        public static SpectrumRendererData Create(IFFTDataTransformer transformer, int width, int height, int[] bands, int fftSize, bool showPeak, bool showRms, int history, IDictionary<string, IntPtr> colors)
        {
            var margin = width > (bands.Length * MARGIN_MIN) ? MARGIN_ONE : MARGIN_ZERO;
            var data = new SpectrumRendererData(history, showPeak, showRms)
            {
                Transformer = transformer,
                Width = width,
                Height = height,
                Margin = margin,
                FFTSize = fftSize,
                Values = new float[bands.Length],
                Colors = colors,
                ValueElements = new Int32Rect[bands.Length],
            };
            if (showPeak)
            {
                data.PeakValues = new float[bands.Length];
                data.PeakElements = new Int32Rect[bands.Length];
            }
            if (showRms)
            {
                data.RmsValues = new float[bands.Length];
                data.RmsElements = new Int32Rect[bands.Length];
            }
            return data;
        }

        public class SpectrumRendererData : FFTVisualizationData
        {
            public SpectrumRendererData(int history, bool showPeak, bool showRms)
            {
                this.History = new VisualizationDataHistory()
                {
                    Capacity = history,
                    Flags = VisualizationDataHistoryFlags.None
                };
                if (showPeak)
                {
                    this.History.Flags |= VisualizationDataHistoryFlags.Peak;
                }
                if (showRms)
                {
                    this.History.Flags |= VisualizationDataHistoryFlags.Rms;
                }
            }

            public IFFTDataTransformer Transformer;

            public float[] Values;

            public float[] PeakValues;

            public float[] RmsValues;

            public int Width;

            public int Height;

            public int Margin;

            public IDictionary<string, IntPtr> Colors;

            public Int32Rect[] ValueElements;

            public Int32Rect[] PeakElements;

            public Int32Rect[] RmsElements;

            public DateTime LastUpdated;

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

            public BitmapHelper.RenderInfo Rms;

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

            public static Color[] GetPeak(Color[] background, bool showRms, Color[] colors)
            {
                var transparency = background.Length > 1 || background.FirstOrDefault().A != 255;
                if (transparency)
                {
                    if (showRms)
                    {
                        return colors.WithAlpha(-200);
                    }
                    else
                    {
                        return colors.WithAlpha(-100);
                    }
                }
                else
                {
                    var color = background.FirstOrDefault();
                    if (showRms)
                    {
                        return colors.Interpolate(color, 0.8f);
                    }
                    else
                    {
                        return colors.Interpolate(color, 0.4f);
                    }
                }
            }

            public static Color[] GetRms(Color[] background, bool showPeak, Color[] colors)
            {
                var transparency = background.Length > 1 || background.FirstOrDefault().A != 255;
                if (transparency)
                {
                    if (showPeak)
                    {
                        return colors.WithAlpha(-100);
                    }
                    else
                    {
                        return colors.WithAlpha(-50);
                    }
                }
                else
                {
                    var color = background.FirstOrDefault();
                    if (showPeak)
                    {
                        return colors.Interpolate(color, 0.4f);
                    }
                    else
                    {
                        return colors.Interpolate(color, 0.2f);
                    }
                }
            }

            public static Color[] GetValue(Color[] colors)
            {
                return colors;
            }

            public static Color[] GetCrest()
            {
                return new[]
                {
                    global::System.Windows.Media.Colors.Red
                };
            }
        }
    }
}
