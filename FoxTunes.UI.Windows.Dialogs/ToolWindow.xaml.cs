using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for ToolWindow.xaml
    /// </summary>
    public partial class ToolWindow : WindowBase
    {
        public ToolWindow(ToolWindowConfiguration configuration) : base(configuration.ApplyTransparency)
        {
            this.InitializeComponent();
            this.Configuration = configuration;
        }

        public override string Id
        {
            get
            {
                var configuration = this.Configuration;
                if (configuration == null)
                {
                    return string.Empty;
                }
                return configuration.Title;
            }
        }

        public ToolWindowConfiguration Configuration
        {
            get
            {
                var viewModel = this.TryFindResource("ViewModel") as global::FoxTunes.ViewModel.ToolWindow;
                if (viewModel == null)
                {
                    return null;
                }
                return viewModel.Configuration;
            }
            private set
            {
                var viewModel = this.TryFindResource("ViewModel") as global::FoxTunes.ViewModel.ToolWindow;
                if (viewModel == null)
                {
                    return;
                }
                viewModel.Configuration = value;
                if (viewModel.Configuration == null)
                {
                    return;
                }
                this.UpdateBounds(viewModel.Bounds);
            }
        }

        protected virtual void UpdateBounds(Rect bounds)
        {
            if (bounds.IsEmpty)
            {
                return;
            }
            if (bounds.Left != 0 && bounds.Right != 0 && ScreenHelper.WindowBoundsVisible(bounds))
            {
                this.Left = bounds.Left;
                this.Top = bounds.Top;
            }
            if (bounds.Width > 0)
            {
                this.Width = bounds.Width;
            }
            if (bounds.Height > 0)
            {
                this.Height = bounds.Height;
            }
        }

        public void UpdateBounds()
        {
            var viewModel = this.TryFindResource("ViewModel") as global::FoxTunes.ViewModel.ToolWindow;
            if (viewModel != null)
            {
                viewModel.Bounds = this.RestoreBounds;
            }
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            this.UpdateBounds();
            base.OnLocationChanged(e);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            this.UpdateBounds();
            base.OnRenderSizeChanged(sizeInfo);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
            base.OnMouseDown(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.UpdateBounds();
            base.OnClosing(e);
        }
    }
}
