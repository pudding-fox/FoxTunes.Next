using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for AsyncImage.xaml
    /// </summary>
    public partial class AsyncImage : ContentControl
    {
        public const int FADE_SPEED = 500;

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source",
            typeof(Brush),
            typeof(AsyncImage),
            new PropertyMetadata(new PropertyChangedCallback(OnSourceChanged))
        );

        public static Brush GetSource(AsyncImage source)
        {
            return (Brush)source.GetValue(SourceProperty);
        }

        public static void SetSource(AsyncImage source, Brush value)
        {
            source.SetValue(SourceProperty, value);
        }

        public static void OnSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var asyncImage = sender as AsyncImage;
            if (asyncImage == null)
            {
                return;
            }
            asyncImage.OnSourceChanged();
        }

        public AsyncImage()
        {
            this.InitializeComponent();
        }

        public Brush Source
        {
            get
            {
                return GetSource(this);
            }
            set
            {
                SetSource(this, value);
            }
        }

        protected virtual void OnSourceChanged()
        {
            this.Update(this.Source);
            if (this.SourceChanged != null)
            {
                this.SourceChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler SourceChanged;

        private FadeDirection Direction;

        public void Update(Brush brush)
        {
            switch (this.Direction)
            {
                case FadeDirection.None:
                    this.A.Fill = brush;
                    this.A.Visibility = Visibility.Visible;
                    this.Direction = FadeDirection.AToB;
                    break;
                case FadeDirection.AToB:
                    this.Update(this.A, this.B, brush);
                    this.Direction = FadeDirection.BToA;
                    break;
                case FadeDirection.BToA:
                    this.Update(this.B, this.A, brush);
                    this.Direction = FadeDirection.AToB;
                    break;
            }
        }

        protected virtual void Update(Rectangle from, Rectangle to, Brush brush)
        {
            to.Fill = brush;
            to.Opacity = 0;
            to.Visibility = Visibility.Visible;

            var fadeOut = new DoubleAnimation();
            fadeOut.To = 0;
            fadeOut.From = 1;
            fadeOut.Duration = TimeSpan.FromMilliseconds(FADE_SPEED);
            fadeOut.EasingFunction = new QuadraticEase();

            var completed = default(EventHandler);
            completed = new EventHandler((sender, e) =>
            {
                from.Opacity = 0;
                from.Visibility = Visibility.Hidden;
                from.Fill = null;
                fadeOut.Completed -= completed;
            });
            fadeOut.Completed += completed;

            var fadeIn = new DoubleAnimation();
            fadeIn.To = 1;
            fadeIn.From = 0;
            fadeIn.Duration = TimeSpan.FromMilliseconds(FADE_SPEED);
            fadeIn.EasingFunction = new QuadraticEase();

            from.BeginAnimation(Rectangle.OpacityProperty, fadeOut);
            to.BeginAnimation(Rectangle.OpacityProperty, fadeIn);
        }

        private enum FadeDirection : byte
        {
            None,
            AToB,
            BToA
        }
    }
}
