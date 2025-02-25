using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FoxTunes
{
    public class WaveFormRenderer : RendererBase
    {
        public object SyncRoot = new object();

        public WaveFormGenerator.WaveFormGeneratorData GeneratorData { get; private set; }

        public WaveFormRendererData RendererData { get; private set; }

        public WaveFormGenerator Generator { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public SelectionConfigurationElement Mode { get; private set; }

        public IntegerConfigurationElement Resolution { get; private set; }

        public BooleanConfigurationElement Rms { get; private set; }

        public BooleanConfigurationElement Logarithmic { get; private set; }

        public IntegerConfigurationElement Smoothing { get; private set; }

        public TextConfigurationElement ColorPalette { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Generator = ComponentRegistry.Instance.GetComponent<WaveFormGenerator>();
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            base.InitializeComponent(core);
        }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Mode = this.Configuration.GetElement<SelectionConfigurationElement>(
                    WaveFormStreamPositionConfiguration.SECTION,
                    WaveFormStreamPositionConfiguration.MODE_ELEMENT
                );
                this.Resolution = this.Configuration.GetElement<IntegerConfigurationElement>(
                    WaveFormGeneratorConfiguration.SECTION,
                    WaveFormGeneratorConfiguration.RESOLUTION_ELEMENT
                );
                this.Rms = this.Configuration.GetElement<BooleanConfigurationElement>(
                    WaveFormStreamPositionConfiguration.SECTION,
                    WaveFormStreamPositionConfiguration.RMS_ELEMENT
                );
                this.Logarithmic = this.Configuration.GetElement<BooleanConfigurationElement>(
                    WaveFormStreamPositionConfiguration.SECTION,
                    WaveFormStreamPositionConfiguration.DB_ELEMENT
                );
                this.Smoothing = this.Configuration.GetElement<IntegerConfigurationElement>(
                    WaveFormStreamPositionConfiguration.SECTION,
                    WaveFormStreamPositionConfiguration.SMOOTHING_ELEMENT
                );
                this.ColorPalette = this.Configuration.GetElement<TextConfigurationElement>(
                    WaveFormStreamPositionConfiguration.SECTION,
                    WaveFormStreamPositionConfiguration.COLOR_PALETTE_ELEMENT
                );
                this.Mode.ValueChanged += this.OnValueChanged;
                this.Resolution.ValueChanged += this.OnValueChanged;
                this.Rms.ValueChanged += this.OnValueChanged;
                this.Logarithmic.ValueChanged += this.OnValueChanged;
                this.Smoothing.ValueChanged += this.OnValueChanged;
                this.ColorPalette.ValueChanged += this.OnValueChanged;
                this.OnCurrentStreamChanged(this, EventArgs.Empty);
            }
            base.OnConfigurationChanged();
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            this.Dispatch(() => this.Update(this.PlaybackManager.CurrentStream));
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            if (object.ReferenceEquals(sender, this.Resolution))
            {
                //Changing resolution requires full refresh.
                this.Dispatch(() => this.Update(this.PlaybackManager.CurrentStream));
            }
            else
            {
                this.Dispatch(this.CreateData);
            }
        }

        protected virtual async Task Update(IOutputStream stream)
        {
            if (this.GeneratorData != null)
            {
                this.GeneratorData.Updated -= this.OnUpdated;
                if (this.GeneratorData.CancellationToken != null)
                {
                    this.GeneratorData.CancellationToken.Cancel();
                }
            }

            if (stream != null)
            {
                this.GeneratorData = this.Generator.Generate(stream);
                this.GeneratorData.Updated += this.OnUpdated;
            }
            else
            {
                this.GeneratorData = WaveFormGenerator.WaveFormGeneratorData.Empty;
                await this.Clear().ConfigureAwait(false);
            }

            await this.RefreshBitmap().ConfigureAwait(false);
        }

        protected virtual void OnUpdated(object sender, EventArgs e)
        {
            this.Dispatch(this.Update);
        }

        protected override bool CreateData(int width, int height)
        {
            if (this.Configuration == null)
            {
                return false;
            }
            var generatorData = this.GeneratorData;
            if (generatorData == null)
            {
                generatorData = WaveFormGenerator.WaveFormGeneratorData.Empty;
            }
            var mode = WaveFormStreamPositionConfiguration.GetMode(this.Mode.Value);
            this.RendererData = Create(
                generatorData,
                width,
                height,
                this.Rms.Value,
                this.Logarithmic.Value,
                this.Smoothing.Value,
                mode,
                this.GetColorPalettes(this.GetColorPaletteOrDefault(this.ColorPalette.Value), this.Rms.Value, generatorData.Channels, mode)
            );
            if (this.RendererData == null)
            {
                return false;
            }
            this.Dispatch(this.Update);
            return true;
        }

        protected virtual IDictionary<string, IntPtr> GetColorPalettes(string value, bool showRms, int channels, WaveFormRendererMode mode)
        {
            var flags = default(int);
            var palettes = WaveFormStreamPositionConfiguration.GetColorPalette(value);
            var background = palettes.GetOrAdd(
                WaveFormStreamPositionConfiguration.COLOR_PALETTE_BACKGROUND,
                () => DefaultColors.GetBackground()
            );
            //Switch the default colors to the VALUE palette if one was provided.
            var colors = palettes.GetOrAdd(
                WaveFormStreamPositionConfiguration.COLOR_PALETTE_VALUE,
                () => DefaultColors.GetValue(new[] { this.ForegroundColor })
            );
            if (showRms)
            {
                palettes.GetOrAdd(
                    WaveFormStreamPositionConfiguration.COLOR_PALETTE_RMS,
                    () => DefaultColors.GetRms(background, colors)
                );
            }
            return palettes.ToDictionary(
                pair => pair.Key,
                pair =>
                {
                    flags = 0;
                    colors = pair.Value;
                    if (colors.Length > 1)
                    {
                        flags |= BitmapHelper.COLOR_FROM_Y;
                        if (new[] { WaveFormStreamPositionConfiguration.COLOR_PALETTE_VALUE, WaveFormStreamPositionConfiguration.COLOR_PALETTE_RMS }.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
                        {
                            colors = colors.MirrorGradient(false);
                            switch (mode)
                            {
                                case WaveFormRendererMode.Seperate:
                                    colors = colors.DuplicateGradient(channels);
                                    break;
                            }
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
                    info = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[WaveFormStreamPositionConfiguration.COLOR_PALETTE_BACKGROUND]);
                }
                else
                {
                    var palettes = this.GetColorPalettes(this.GetColorPaletteOrDefault(this.ColorPalette.Value), false, 0, WaveFormRendererMode.None);
                    info = BitmapHelper.CreateRenderInfo(bitmap, palettes[WaveFormStreamPositionConfiguration.COLOR_PALETTE_BACKGROUND]);
                }
                BitmapHelper.DrawRectangle(ref info, 0, 0, data.Width, data.Height);
                bitmap.AddDirtyRect(new global::System.Windows.Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
            this.Dispatch(this.Update);
        }

        public Task Render(WaveFormRendererData data)
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
                var info = GetRenderInfo(bitmap, data);
                Monitor.Enter(this.SyncRoot);
                try
                {
                    Render(ref info, data);
                }
                catch (Exception e)
                {
                    Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render wave form: {0}", e.Message);
                }
                finally
                {
                    Monitor.Exit(this.SyncRoot);
                }
                bitmap.AddDirtyRect(new global::System.Windows.Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                bitmap.Unlock();
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
                        Logger.Write(this.GetType(), LogLevel.Warn, "Failed to update wave form data: {0}", e.Message);
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
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            }
            if (this.GeneratorData != null)
            {
                this.GeneratorData.Updated -= this.OnUpdated;
            }
            if (this.Mode != null)
            {
                this.Mode.ValueChanged -= this.OnValueChanged;
            }
            if (this.Resolution != null)
            {
                this.Resolution.ValueChanged -= this.OnValueChanged;
            }
            if (this.Rms != null)
            {
                this.Rms.ValueChanged -= this.OnValueChanged;
            }
            if (this.Logarithmic != null)
            {
                this.Logarithmic.ValueChanged -= this.OnValueChanged;
            }
            if (this.Smoothing != null)
            {
                this.Smoothing.ValueChanged -= this.OnValueChanged;
            }
            if (this.ColorPalette != null)
            {
                this.ColorPalette.ValueChanged -= this.OnValueChanged;
            }
            base.OnDisposing();
        }

        private static void Update(WaveFormGenerator.WaveFormGeneratorData generatorData, WaveFormRendererData rendererData)
        {
            if (rendererData.Channels == 1)
            {
                UpdateViewMono(generatorData, rendererData);
                UpdateMono(rendererData);
            }
            else if (rendererData.Channels > 1)
            {
                UpdateViewSeperate(generatorData, rendererData);
                UpdateSeperate(rendererData);
            }
        }

        private static void UpdateViewMono(WaveFormGenerator.WaveFormGeneratorData generatorData, WaveFormRendererData rendererData)
        {
            var logarithmic = rendererData.Logarithmic;
            var rms = rendererData.View.Rms != null;
            var valuesPerElement = rendererData.ValuesPerElement;

            for (; rendererData.View.Position < rendererData.Width; rendererData.View.Position++)
            {
                var valuePosition = rendererData.View.Position * rendererData.ValuesPerElement;
                if ((valuePosition + rendererData.ValuesPerElement) > generatorData.Position)
                {
                    break;
                }

                var value = default(float);
                for (var a = 0; a < valuesPerElement; a++)
                {
                    for (var b = 0; b < rendererData.Channels; b++)
                    {
                        value += Math.Max(
                            Math.Abs(
                                generatorData.Data[valuePosition + a, b].Min
                            ),
                            generatorData.Data[valuePosition + a, b].Max
                        );
                    }
                }
                value /= (valuesPerElement * rendererData.Channels);

                if (logarithmic)
                {
                    value = ToDecibelFixed(value);
                }

                value = Math.Min(value, 1);

                rendererData.View.Peak = Math.Max(value, rendererData.View.Peak);
                rendererData.View.Data[0, rendererData.View.Position] = value;

                if (rms)
                {
                    for (var a = 0; a < valuesPerElement; a++)
                    {
                        for (var b = 0; b < rendererData.Channels; b++)
                        {
                            value += generatorData.Data[valuePosition + a, b].Rms;
                        }
                    }
                    value /= (valuesPerElement * rendererData.Channels);

                    if (logarithmic)
                    {
                        value = ToDecibelFixed(value);
                    }

                    value = Math.Min(value, 1);

                    rendererData.View.Rms[0, rendererData.View.Position] = value;
                }
            }
            if (rendererData.Smoothing > 0)
            {
                if (generatorData.Position == generatorData.Capacity)
                {
                    rendererData.View.Peak = NoiseReduction(rendererData.View.Data, 1, rendererData.Width, rendererData.Smoothing);
                    if (rms)
                    {
                        NoiseReduction(rendererData.View.Rms, 1, rendererData.Width, rendererData.Smoothing);
                    }
                    rendererData.Position = 0;
                }
            }
        }

        private static void UpdateViewSeperate(WaveFormGenerator.WaveFormGeneratorData generatorData, WaveFormRendererData rendererData)
        {
            var logarithmic = rendererData.Logarithmic;
            var rms = rendererData.View.Rms != null;
            var valuesPerElement = rendererData.ValuesPerElement;

            for (; rendererData.View.Position < rendererData.Width; rendererData.View.Position++)
            {
                var valuePosition = rendererData.View.Position * rendererData.ValuesPerElement;
                if ((valuePosition + rendererData.ValuesPerElement) > generatorData.Position)
                {
                    break;
                }

                var value = default(float);
                for (var channel = 0; channel < rendererData.Channels; channel++)
                {
                    for (var a = 0; a < valuesPerElement; a++)
                    {
                        value += Math.Max(
                            Math.Abs(
                                generatorData.Data[valuePosition + a, channel].Min
                            ),
                            generatorData.Data[valuePosition + a, channel].Max
                        );
                    }
                    value /= valuesPerElement;

                    if (logarithmic)
                    {
                        value = ToDecibelFixed(value);
                    }

                    value = Math.Min(value, 1);

                    rendererData.View.Peak = Math.Max(value, rendererData.View.Peak);
                    rendererData.View.Data[channel, rendererData.View.Position] = value;

                    if (rms)
                    {
                        for (var a = 0; a < valuesPerElement; a++)
                        {
                            value += generatorData.Data[valuePosition + a, channel].Rms;
                        }
                        value /= valuesPerElement;

                        if (logarithmic)
                        {
                            value = ToDecibelFixed(value);
                        }

                        value = Math.Min(value, 1);

                        rendererData.View.Rms[channel, rendererData.View.Position] = value;
                    }
                }
            }
            if (rendererData.Smoothing > 0)
            {
                if (generatorData.Position == generatorData.Capacity)
                {
                    rendererData.View.Peak = NoiseReduction(rendererData.View.Data, rendererData.Channels, rendererData.Width, rendererData.Smoothing);
                    if (rms)
                    {
                        NoiseReduction(rendererData.View.Rms, rendererData.Channels, rendererData.Width, rendererData.Smoothing);
                    }
                    rendererData.Position = 0;
                }
            }
        }

        private static void UpdateMono(WaveFormRendererData rendererData)
        {
            var center = rendererData.Height / 2.0f;
            var data = rendererData.View.Data;
            var rms = rendererData.View.Rms;
            var peak = rendererData.View.Peak;
            var waveElements = rendererData.WaveElements;
            var powerElements = rendererData.PowerElements;

            if (peak == 0)
            {
                peak = 1;
            }

            while (rendererData.Position < rendererData.View.Position)
            {
                {
                    var value = data[0, rendererData.Position] / peak;
                    var y = Convert.ToInt32(center - (value * center));
                    var height = Math.Max(Convert.ToInt32((center - y) + (value * center)), 1);

                    waveElements[0, rendererData.Position].X = rendererData.Position;
                    waveElements[0, rendererData.Position].Y = y;
                    waveElements[0, rendererData.Position].Width = 1;
                    waveElements[0, rendererData.Position].Height = height;

                }

                if (powerElements != null)
                {
                    var value = rms[0, rendererData.Position] / peak;
                    var y = Convert.ToInt32(center - (value * center));
                    var height = Math.Max(Convert.ToInt32((center - y) + (value * center)), 1);

                    powerElements[0, rendererData.Position].X = rendererData.Position;
                    powerElements[0, rendererData.Position].Y = y;
                    powerElements[0, rendererData.Position].Width = 1;
                    powerElements[0, rendererData.Position].Height = height;

                }

                rendererData.Position++;
            }
        }

        private static void UpdateSeperate(WaveFormRendererData rendererData)
        {
            var data = rendererData.View.Data;
            var rms = rendererData.View.Rms;
            var peak = rendererData.View.Peak;
            var waveElements = rendererData.WaveElements;
            var powerElements = rendererData.PowerElements;
            var waveHeight = rendererData.Height / rendererData.Channels;

            if (peak == 0)
            {
                peak = 1;
            }

            while (rendererData.Position < rendererData.View.Position)
            {
                for (var channel = 0; channel < rendererData.Channels; channel++)
                {
                    var waveCenter = (waveHeight * channel) + (waveHeight / 2);

                    {
                        var value = data[channel, rendererData.Position] / peak;
                        var y = Convert.ToInt32(waveCenter - (value * (waveHeight / 2)));
                        var height = Math.Max(Convert.ToInt32((waveCenter - y) + (value * (waveHeight / 2))), 1);

                        waveElements[channel, rendererData.Position].X = rendererData.Position;
                        waveElements[channel, rendererData.Position].Y = y;
                        waveElements[channel, rendererData.Position].Width = 1;
                        waveElements[channel, rendererData.Position].Height = height;

                    }

                    if (powerElements != null)
                    {
                        var value = rms[channel, rendererData.Position] / peak;
                        var y = Convert.ToInt32(waveCenter - (value * (waveHeight / 2)));
                        var height = Math.Max(Convert.ToInt32((waveCenter - y) + (value * (waveHeight / 2))), 1);

                        powerElements[channel, rendererData.Position].X = rendererData.Position;
                        powerElements[channel, rendererData.Position].Y = y;
                        powerElements[channel, rendererData.Position].Width = 1;
                        powerElements[channel, rendererData.Position].Height = height;

                    }
                }

                rendererData.Position++;
            }
        }

        private static WaveFormRenderInfo GetRenderInfo(WriteableBitmap bitmap, WaveFormRendererData data)
        {
            var info = new WaveFormRenderInfo()
            {
                Background = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[WaveFormStreamPositionConfiguration.COLOR_PALETTE_BACKGROUND])
            };
            if (data.PowerElements != null)
            {
                info.Rms = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[WaveFormStreamPositionConfiguration.COLOR_PALETTE_RMS]);
            }
            if (data.WaveElements != null)
            {
                info.Value = BitmapHelper.CreateRenderInfo(bitmap, data.Colors[WaveFormStreamPositionConfiguration.COLOR_PALETTE_VALUE]);
            }
            return info;
        }

        public static void Render(ref WaveFormRenderInfo info, WaveFormRendererData data)
        {
            if (info.Background.Width != data.Width || info.Background.Height != data.Height)
            {
                //Bitmap does not match data.
                return;
            }
            BitmapHelper.DrawRectangle(ref info.Background, 0, 0, data.Width, data.Height);

            if (data.Position == 0)
            {
                //No data.
                return;
            }

            if (data.WaveElements != null)
            {
                BitmapHelper.DrawRectangles(ref info.Value, data.WaveElements, data.WaveElements.Length);
            }
            if (data.PowerElements != null)
            {
                BitmapHelper.DrawRectangles(ref info.Rms, data.PowerElements, data.PowerElements.Length);
            }
        }

        public static WaveFormRendererData Create(WaveFormGenerator.WaveFormGeneratorData generatorData, int width, int height, bool rms, bool logarithmic, int smoothing, WaveFormRendererMode mode, IDictionary<string, IntPtr> colors)
        {
            var valuesPerElement = generatorData.Capacity / width;
            if (valuesPerElement == 0)
            {
                valuesPerElement = 1;
            }
            var data = new WaveFormRendererData()
            {
                Width = width,
                Height = height,
                Logarithmic = logarithmic,
                Smoothing = smoothing,
                ValuesPerElement = valuesPerElement,
                Colors = colors,
                Capacity = width,
                View = new WaveFormGeneratorDataView()
            };
            switch (mode)
            {
                default:
                case WaveFormRendererMode.Mono:
                    data.WaveElements = new Int32Rect[1, width];
                    data.View.Data = new float[1, width];
                    if (rms)
                    {
                        data.PowerElements = new Int32Rect[1, width];
                        data.View.Rms = new float[1, width];
                    }
                    data.Channels = 1;
                    break;
                case WaveFormRendererMode.Seperate:
                    data.WaveElements = new Int32Rect[generatorData.Channels, width];
                    data.View.Data = new float[generatorData.Channels, width];
                    if (rms)
                    {
                        data.PowerElements = new Int32Rect[generatorData.Channels, width];
                        data.View.Rms = new float[generatorData.Channels, width];
                    }
                    data.Channels = generatorData.Channels;
                    break;
            }
            return data;
        }

        public class WaveFormGeneratorDataView
        {
            public float[,] Data;

            public float[,] Rms;

            public float Peak;

            public int Position;
        }

        public class WaveFormRendererData
        {
            public int Width;

            public int Height;

            public bool Logarithmic;

            public int Smoothing;

            public int ValuesPerElement;

            public IDictionary<string, IntPtr> Colors;

            public Int32Rect[,] WaveElements;

            public Int32Rect[,] PowerElements;

            public int Channels;

            public int Position;

            public int Capacity;

            public WaveFormGeneratorDataView View;

            ~WaveFormRendererData()
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
        public struct WaveFormRenderInfo
        {
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

            public static Color[] GetRms(Color[] background, Color[] colors)
            {
                const byte SHADE = 30;
                var contrast = Color.FromRgb(SHADE, SHADE, SHADE);
                return colors.Shade(contrast);
            }

            public static Color[] GetValue(Color[] colors)
            {
                return colors;
            }
        }
    }

    public enum WaveFormRendererMode : byte
    {
        None,
        Mono,
        Seperate
    }
}
