using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FoxTunes
{
    public static partial class ImageExtensions
    {
        private static readonly ConditionalWeakTable<Image, GifBehaviour> GifBehaviours = new ConditionalWeakTable<Image, GifBehaviour>();

        public static readonly DependencyProperty GifProperty = DependencyProperty.RegisterAttached(
            "Gif",
            typeof(Uri),
            typeof(ImageExtensions),
            new PropertyMetadata(default(Uri), new PropertyChangedCallback(OnGifPropertyChanged))
        );

        public static Uri GetGif(Image source)
        {
            return (Uri)source.GetValue(GifProperty);
        }

        public static void SetGif(Image source, Uri value)
        {
            source.SetValue(GifProperty, value);
        }

        private static void OnGifPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var image = sender as Image;
            if (image == null)
            {
                return;
            }
            var behaviour = default(GifBehaviour);
            var uri = GetGif(image);
            if (GifBehaviours.TryRemove(image, out behaviour))
            {
                behaviour.Dispose();
            }
            if (uri != default(Uri))
            {
                GifBehaviours.Add(image, new GifBehaviour(image, uri));
            }
        }

        private class GifBehaviour : UIBehaviour<Image>
        {
            public GifBehaviour(Image image, Uri uri) : base(image)
            {
                this.Image = image;
                this.Decoder = new GifBitmapDecoder(uri, BitmapCreateOptions.None, BitmapCacheOption.Default);
                this.Index = 0;
                this.Timer = new DispatcherTimer();
                this.Timer.Interval = TimeSpan.Zero;
                this.Timer.Tick += this.OnTick;
                this.Timer.Start();
            }

            public Image Image { get; private set; }

            public GifBitmapDecoder Decoder { get; private set; }

            public DispatcherTimer Timer { get; private set; }

            public int Index { get; private set; }

            protected virtual void OnTick(object sender, EventArgs e)
            {
                if (this.Index < this.Decoder.Frames.Count - 1)
                {
                    this.Index++;
                }
                else
                {
                    this.Index = 0;
                }
                var frame = this.Decoder.Frames[this.Index];
                var metaData = frame.Metadata as BitmapMetadata;
                this.Image.Source = frame;
                this.Timer.Interval = this.GetFrameDelay(frame, metaData);
            }

            protected virtual TimeSpan GetFrameDelay(BitmapFrame frame, BitmapMetadata metaData)
            {
                var delay = Convert.ToInt32(metaData.GetQuery("/grctlext/Delay") ?? 10);
                return TimeSpan.FromMilliseconds(delay * 10);
            }

            protected override void OnDisposing()
            {
                if (this.Timer != null)
                {
                    this.Timer.Stop();
                    this.Timer.Tick -= this.OnTick;
                }
                base.OnDisposing();
            }
        }
    }
}
