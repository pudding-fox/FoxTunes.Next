using FoxTunes.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media.Imaging;

namespace FoxTunes
{
    public class SpectrogramRenderer : VisualizationBase
    {
        public SpectrogramRendererData RendererData { get; private set; }

        public SpectrogramRendererHistory RendererHistory { get; private set; }

        public SelectionConfigurationElement Mode { get; private set; }

        public SelectionConfigurationElement Scale { get; private set; }

        public IntegerConfigurationElement Smoothing { get; private set; }

        public TextConfigurationElement ColorPalette { get; private set; }

        public IntegerConfigurationElement History { get; private set; }

        public SelectionConfigurationElement FFTSize { get; private set; }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Mode = this.Configuration.GetElement<SelectionConfigurationElement>(
                    SpectrogramConfiguration.SECTION,
                    SpectrogramConfiguration.MODE_ELEMENT
                );
                this.Scale = this.Configuration.GetElement<SelectionConfigurationElement>(
                    SpectrogramConfiguration.SECTION,
                    SpectrogramConfiguration.SCALE_ELEMENT
                );
                this.Smoothing = this.Configuration.GetElement<IntegerConfigurationElement>(
                    SpectrogramConfiguration.SECTION,
                    SpectrogramConfiguration.SMOOTHING_ELEMENT
                );
                this.ColorPalette = this.Configuration.GetElement<TextConfigurationElement>(
                    SpectrogramConfiguration.SECTION,
                    SpectrogramConfiguration.COLOR_PALETTE_ELEMENT
                );
                this.History = this.Configuration.GetElement<IntegerConfigurationElement>(
                   SpectrogramConfiguration.SECTION,
                   SpectrogramConfiguration.HISTORY_ELEMENT
                );
                this.FFTSize = this.Configuration.GetElement<SelectionConfigurationElement>(
                   SpectrogramConfiguration.SECTION,
                   VisualizationBehaviourConfiguration.FFT_SIZE_ELEMENT
                );
                this.Mode.ValueChanged += this.OnValueChanged;
                this.Scale.ValueChanged += this.OnValueChanged;
                this.Smoothing.ValueChanged += this.OnValueChanged;
                this.ColorPalette.ValueChanged += this.OnValueChanged;
                this.History.ValueChanged += this.OnValueChanged;
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
            var count = default(int);
            var colors = this.GetColorPalette(this.GetColorPaletteOrDefault(this.ColorPalette.Value), out count);
            this.RendererData = Create(
                width,
                height,
                count,
                VisualizationBehaviourConfiguration.GetFFTSize(this.FFTSize.Value),
                colors,
                SpectrogramConfiguration.GetMode(this.Mode.Value),
                SpectrogramConfiguration.GetScale(this.Scale.Value),
                this.Smoothing.Value
            );
            if (this.History.Value > 0)
            {
                if (this.RendererHistory == null || this.RendererHistory.Capacity != this.History.Value)
                {
                    this.RendererHistory = Create(
                         this.History.Value
                    );
                }
            }
            else
            {
                this.RendererHistory = null;
            }
            return true;
        }

        protected virtual IntPtr GetColorPalette(string value, out int count)
        {
            var palette = SpectrogramConfiguration.GetColorPalette(value);
            count = palette.Length;
            return BitmapHelper.CreatePalette(0, false, palette);
        }

        protected override WriteableBitmap CreateBitmap(int width, int height)
        {
            var bitmap = base.CreateBitmap(width, height);
            var data = this.RendererData;
            var history = this.RendererHistory;
            if (data != null && history != null && history.Count > 0)
            {
                try
                {
                    if (bitmap.TryLock(LockTimeout))
                    {
                        try
                        {
                            var info = BitmapHelper.CreateRenderInfo(bitmap, data.Colors);
                            BitmapHelper.DrawRectangle(ref info, 0, 0, info.Width, info.Height);
                            lock (history)
                            {
                                Restore(info, data, history);
                            }
                            bitmap.AddDirtyRect(new global::System.Windows.Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                        }
                        finally
                        {
                            bitmap.Unlock();
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Error, "Failed to restore spectrogram from history: {0}", e.Message);
                }
            }
            else
            {
                this.ClearBitmap(bitmap);
            }
            return bitmap;
        }

        protected override void ClearBitmap(WriteableBitmap bitmap)
        {
            var data = this.RendererData;
            if (data == null)
            {
                return;
            }
            if (!bitmap.TryLock(LockTimeout))
            {
                return;
            }
            try
            {
                var info = BitmapHelper.CreateRenderInfo(bitmap, data.Colors);
                BitmapHelper.DrawRectangle(ref info, 0, 0, info.Width, info.Height);
                bitmap.AddDirtyRect(new global::System.Windows.Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        protected virtual Task Render(SpectrogramRendererData data)
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
                var info = BitmapHelper.CreateRenderInfo(bitmap, data.Colors);
                try
                {
                    lock (data)
                    {
                        Render(info, data);
                        success = true;
                    }
                }
                catch (Exception e)
                {
#if DEBUG
                    Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render spectrogram: {0}", e.Message);
#else
                    Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render spectrogram, disabling: {0}", e.Message);
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
            var history = this.RendererHistory;
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
                if (history != null)
                {
                    lock (history)
                    {
                        history.Write(data);
                    }
                }
                UpdateValues(data);
                UpdateElements(data);

                this.BeginUpdateData();
            }
            catch (Exception exception)
            {
#if DEBUG
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to update spectrogram data: {0}", exception.Message);
                this.BeginUpdateData();
#else
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to update spectrogram data, disabling: {0}", exception.Message);
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
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render spectrogram data: {0}", exception.Message);
                this.BeginUpdateData();
#else
                Logger.Write(this.GetType(), LogLevel.Warn, "Failed to render spectrogram data, disabling: {0}", exception.Message);
#endif
            }
        }

        protected override void OnDisposing()
        {
            if (this.Mode != null)
            {
                this.Mode.ValueChanged -= this.OnValueChanged;
            }
            if (this.Scale != null)
            {
                this.Scale.ValueChanged -= this.OnValueChanged;
            }
            if (this.Smoothing != null)
            {
                this.Smoothing.ValueChanged -= this.OnValueChanged;
            }
            if (this.ColorPalette != null)
            {
                this.ColorPalette.ValueChanged -= this.OnValueChanged;
            }
            if (this.History != null)
            {
                this.History.ValueChanged -= this.OnValueChanged;
            }
            if (this.FFTSize != null)
            {
                this.FFTSize.ValueChanged -= this.OnValueChanged;
            }
            base.OnDisposing();
        }

        private static void Render(BitmapHelper.RenderInfo info, SpectrogramRendererData data)
        {
            if (data.SampleCount == 0 || !data.Initialized)
            {
                //No data.
                return;
            }

            if (info.Width != data.Width || info.Height != data.Height)
            {
                //Bitmap does not match data.
                return;
            }

            BitmapHelper.ShiftLeft(ref info, 1);
            BitmapHelper.DrawDots(ref info, data.Elements, data.Elements.Length);
        }

        private static void UpdateValues(SpectrogramRendererData data)
        {
            switch (data.Mode)
            {
                default:
                case SpectrogramRendererMode.Mono:
                    UpdateValuesMono(data.Width, data.Height, data.Samples, data.Values, data.Scale, data.Smoothing);
                    break;
                case SpectrogramRendererMode.Seperate:
                    UpdateValuesSeperate(data.Width, data.Height, data.Samples, data.Values, data.Channels, data.Scale, data.Smoothing);
                    break;
            }
        }

        private static void UpdateValuesMono(int width, int height, float[] samples, float[,] values, SpectrogramRendererScale scale, int smoothing)
        {
            var num1 = 0f;
            var num2 = 0f;
            var num3 = 0f;
            var num4 = 0f;
            var num5 = (float)(samples.Length - 1) / (height - 1);

            Array.Clear(values, 0, values.Length);

            for (var a = 0; a < samples.Length; a++)
            {
                switch (scale)
                {
                    default:
                    case SpectrogramRendererScale.Linear:
                        num1 = (float)a / num5;
                        break;
                    case SpectrogramRendererScale.Logarithmic:
                        num1 = (float)a / (samples.Length - 1);
                        num1 = Convert.ToSingle(1 - Math.Pow(1 - num1, 5));
                        num1 = num1 * (height - 1);
                        break;
                }
                num3 = ToDecibelFixed(samples[a]);
                num4 = Math.Max(num3, num4);
                num4 = Math.Min(num4, 1);
                num4 = Math.Max(num4, 0);
                if (num1 > num2)
                {
                    for (; num2 < num1; num2++)
                    {
                        values[0, Convert.ToInt32(num2)] = num4;
                    }
                    num4 = 0;
                }
            }
            if (smoothing > 0)
            {
                NoiseReduction(values, 1, height, smoothing);
            }
        }

        private static void UpdateValuesSeperate(int width, int height, float[] samples, float[,] values, int channels, SpectrogramRendererScale scale, int smoothing)
        {
            var num1 = 0f;
            var num2 = 0f;
            var num3 = 0f;
            var num4 = 0f;
            var num5 = (float)samples.Length / ((height / channels) - 1);

            Array.Clear(values, 0, values.Length);

            for (var channel = 0; channel < channels; channel++)
            {
                num2 = 0f;
                for (var a = channel; a < samples.Length; a += channels)
                {
                    switch (scale)
                    {
                        default:
                        case SpectrogramRendererScale.Linear:
                            num1 = (float)a / num5;
                            break;
                        case SpectrogramRendererScale.Logarithmic:
                            num1 = (float)a / (samples.Length - 1);
                            num1 = Convert.ToSingle(1 - Math.Pow(1 - num1, 5));
                            num1 = num1 * ((height / channels) - 1);
                            break;
                    }
                    num3 = ToDecibelFixed(samples[a]);
                    num4 = Math.Max(num3, num4);
                    num4 = Math.Max(num4, 0);
                    if (num1 > num2)
                    {
                        for (; num2 < num1; num2++)
                        {
                            values[channel, Convert.ToInt32(num2)] = num4;
                        }
                        num4 = 0;
                    }
                }
            }
            if (smoothing > 0)
            {
                NoiseReduction(values, channels, height, smoothing);
            }
        }

        private static void UpdateElements(SpectrogramRendererData data)
        {
            switch (data.Mode)
            {
                default:
                case SpectrogramRendererMode.Mono:
                    UpdateElementsMono(data.Values, data.Elements, data.Count, data.Width - 1, data.Height);
                    break;
                case SpectrogramRendererMode.Seperate:
                    UpdateElementsSeperate(data.Values, data.Elements, data.Count, data.Width - 1, data.Height, data.Channels);
                    break;
            }
        }

        private static void UpdateElementsMono(float[,] values, Int32Pixel[] elements, int count, int x, int height)
        {
            var h = height - 1;
            for (var y = 0; y < height; y++)
            {
                var value1 = values[0, h - y];
                var value2 = Convert.ToInt32(value1 * (count - 1));
                elements[y].X = x;
                elements[y].Y = y;
                elements[y].Color = value2;
            }
        }

        private static void UpdateElementsSeperate(float[,] values, Int32Pixel[] elements, int count, int x, int height, int channels)
        {
            var h = (height - 1) / channels;
            for (var channel = 0; channel < channels; channel++)
            {
                var offset = channel * h;
                for (var y = 0; y < h; y++)
                {
                    var value1 = values[channel, h - y];
                    var value2 = Convert.ToInt32(value1 * (count - 1));
                    elements[offset + y].X = x;
                    elements[offset + y].Y = offset + y;
                    elements[offset + y].Color = value2;
                }
            }
        }

        private static void Restore(BitmapHelper.RenderInfo info, SpectrogramRendererData data, SpectrogramRendererHistory history)
        {
            data.Channels = history.Channels;
            data.Elements = new Int32Pixel[data.Height];

            //TODO: Only realloc if required.
            switch (data.Mode)
            {
                default:
                case SpectrogramRendererMode.Mono:
                    data.Values = new float[1, data.Height];
                    break;
                case SpectrogramRendererMode.Seperate:
                    data.Values = new float[data.Channels, data.Height];
                    break;
            }

            var position = history.Position - 1;
            var count = Math.Min(data.Width, history.Count);
            var elements = new Int32Pixel[count * info.Height];
            for (int a = 0, x = data.Width - 1; a < count; a++, x--)
            {
                history.Read(data, position);

                UpdateValues(data);

                switch (data.Mode)
                {
                    default:
                    case SpectrogramRendererMode.Mono:
                        UpdateElementsMono(data.Values, data.Elements, data.Count, x, data.Height);
                        break;
                    case SpectrogramRendererMode.Seperate:
                        UpdateElementsSeperate(data.Values, data.Elements, data.Count, x, data.Height, data.Channels);
                        break;
                }

                Array.Copy(data.Elements, 0, elements, a * data.Elements.Length, data.Elements.Length);

                if (position > 0)
                {
                    position--;
                }
                else
                {
                    position = count - 1;
                }
            }

            BitmapHelper.DrawDots(ref info, elements, elements.Length);
        }

        public static SpectrogramRendererData Create(int width, int height, int count, int fftSize, IntPtr colors, SpectrogramRendererMode mode, SpectrogramRendererScale scale, int smoothing)
        {
            var data = new SpectrogramRendererData()
            {
                Width = width,
                Height = height,
                Count = count,
                FFTSize = fftSize,
                Colors = colors,
                Elements = new Int32Pixel[height],
                Mode = mode,
                Scale = scale,
                Smoothing = smoothing,
                Flags = mode.HasFlag(SpectrogramRendererMode.Seperate) ? VisualizationDataFlags.Individual : VisualizationDataFlags.None
            };
            return data;
        }

        public class SpectrogramRendererData : FFTVisualizationData
        {
            public int Width;

            public int Height;

            public int Count;

            public float[,] Values;

            public IntPtr Colors;

            public Int32Pixel[] Elements;

            public SpectrogramRendererMode Mode;

            public SpectrogramRendererScale Scale;

            public int Smoothing;

            public override void OnAllocated()
            {
                //TODO: Only realloc if required.
                switch (this.Mode)
                {
                    default:
                    case SpectrogramRendererMode.Mono:
                        this.Values = new float[1, this.Height];
                        break;
                    case SpectrogramRendererMode.Seperate:
                        this.Values = new float[this.Channels, this.Height];
                        break;
                }
                base.OnAllocated();
            }

            ~SpectrogramRendererData()
            {
                try
                {
                    BitmapHelper.DestroyPalette(ref this.Colors);
                }
                catch
                {
                    //Nothing can be done, never throw on GC thread.
                }
            }
        }

        public static SpectrogramRendererHistory Create(int history)
        {
            return new SpectrogramRendererHistory()
            {
                Capacity = history
            };
        }

        public class SpectrogramRendererHistory
        {
            public float[,] Samples;

            public int Channels;

            public int Position;

            public int Count;

            public int Capacity;

            public void Read(SpectrogramRendererData data, int position)
            {
                if (data.Samples == null)
                {
                    data.Samples = new float[this.Samples.GetLength(0)];
                }

                //TODO: Can't find an Array.Copy which can handle the dimention difference.
                for (var a = 0; a < data.Samples.Length; a++)
                {
                    data.Samples[a] = this.Samples[a, position];
                }
            }

            public void Write(SpectrogramRendererData data)
            {
                if (this.Capacity > 0)
                {
                    this.Channels = data.Channels;
                    if (this.Samples == null || this.Samples.GetLength(0) != data.Samples.Length || this.Samples.GetLength(1) != this.Capacity)
                    {
                        this.Position = 0;
                        this.Count = 0;
                        this.Samples = new float[data.Samples.Length, this.Capacity];
                    }
                }
                else
                {
                    if (this.Samples != null)
                    {
                        this.Position = 0;
                        this.Count = 0;
                        this.Samples = null;
                    }
                    return;
                }

                //TODO: Can't find an Array.Copy which can handle the dimention difference.
                for (var a = 0; a < data.Samples.Length; a++)
                {
                    this.Samples[a, this.Position] = data.Samples[a];
                }
                if (this.Position < this.Capacity - 1)
                {
                    this.Position++;
                }
                else
                {
                    this.Position = 0;
                }
                if (this.Count < this.Capacity)
                {
                    this.Count++;
                }
            }
        }
    }

    public enum SpectrogramRendererMode : byte
    {
        None,
        Mono,
        Seperate
    }

    public enum SpectrogramRendererScale : byte
    {
        None,
        Linear,
        Logarithmic
    }
}
