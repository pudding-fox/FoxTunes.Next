using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class ImageResizer : StandardComponent, IDisposable
    {
        private static readonly string PREFIX = typeof(ImageResizer).Name;

        public ISignalEmitter SignalEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.ImagesUpdated:
                    this.Clear();
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public string Resize(string fileName, int width, int height, bool preserveAspectRatio)
        {
            var id = this.GetImageId(fileName, width, height);
            return this.Resize(id, () => Bitmap.FromFile(fileName), width, height, preserveAspectRatio);
        }

        protected virtual string Resize(string id, Func<Image> factory, int width, int height, bool preserveAspectRatio)
        {
            return FileMetaDataStore.IfNotExists(PREFIX, id, result =>
            {
                using (var image = new Bitmap(width, height))
                {
                    using (var graphics = Graphics.FromImage(image))
                    {
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        this.Resize(graphics, factory, width, height, preserveAspectRatio);
                    }
                    using (var stream = new MemoryStream())
                    {
                        image.Save(stream, ImageFormat.Png);
                        stream.Seek(0, SeekOrigin.Begin);
                        return FileMetaDataStore.Write(PREFIX, id, stream);
                    }
                }
            });
        }

        protected virtual void Resize(Graphics graphics, Func<Image> factory, int width, int height, bool preserveAspectRatio)
        {
            using (var image = factory())
            {
                if (preserveAspectRatio)
                {
                    var ratioX = (double)width / (double)image.Width;
                    var ratioY = (double)height / (double)image.Height;
                    var ratio = ratioX < ratioY ? ratioX : ratioY;
                    graphics.DrawImage(
                        image, 
                        new Rectangle(
                            Convert.ToInt32((width - (image.Width * ratio)) / 2), 
                            Convert.ToInt32((height - (image.Height * ratio)) / 2), 
                            Convert.ToInt32(image.Width * ratio), 
                            Convert.ToInt32(image.Height * ratio)
                        )
                    );
                }
                else
                {
                    graphics.DrawImage(image, new Rectangle(0, 0, width, height));
                }
            }
        }

        public Color GetMainColor(string fileName)
        {
            const int MAX_WIDTH = 128;
            const int MAX_HEIGHT = 128;
            var bitmap = Bitmap.FromFile(fileName) as Bitmap;
            if (bitmap.Width > MAX_WIDTH || bitmap.Height > MAX_HEIGHT)
            {
                bitmap = Bitmap.FromFile(
                    this.Resize(
                        fileName,
                        () => bitmap,
                        MAX_WIDTH,
                        MAX_HEIGHT,
                        false
                    )
                ) as Bitmap;
            }
            var colors = new Dictionary<Color, int>();
            for (int x = 0, w = bitmap.Width, h = bitmap.Height; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var color = bitmap.GetPixel(x, y);
                    colors[color] = colors.GetOrAdd(color, 0) + 1;
                }
            }
            return colors
                .OrderBy(pair => pair.Value)
                .Select(pair => pair.Key)
                .FirstOrDefault();
        }


        private string GetImageId(string fileName, int width, int height)
        {
            var hashCode = default(int);
            unchecked
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    hashCode += fileName.GetDeterministicHashCode();
                }
                hashCode = (hashCode * 29) + width.GetHashCode();
                hashCode = (hashCode * 29) + height.GetHashCode();
            }
            return Math.Abs(hashCode).ToString();
        }

        public void Clear()
        {
            try
            {
                FileMetaDataStore.Clear(PREFIX);
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to clear storage \"{0}\": {1}", PREFIX, e.Message);
            }
            finally
            {
                this.OnCleared();
            }
        }

        protected virtual void OnCleared()
        {
            if (this.Cleared != null)
            {
                this.Cleared(this, EventArgs.Empty);
            }
        }

        public event EventHandler Cleared;

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
        }

        ~ImageResizer()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
