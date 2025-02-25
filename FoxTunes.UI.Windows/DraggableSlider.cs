using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace FoxTunes
{
    public class DraggableSlider : Slider
    {
        public DraggableSlider()
        {
            this.IsMoveToPointEnabled = true;
        }

        public Track Track { get; private set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.Track = this.Template.FindName("PART_Track", this) as Track;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.Value = this.Track.ValueFromPoint(e.GetPosition(this.Track));
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (e.ChangedButton == MouseButton.Left)
            {
                ((UIElement)e.OriginalSource).CaptureMouse();
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);
            if (e.ChangedButton == MouseButton.Left)
            {
                ((UIElement)e.OriginalSource).ReleaseMouseCapture();
            }
        }
    }
}
