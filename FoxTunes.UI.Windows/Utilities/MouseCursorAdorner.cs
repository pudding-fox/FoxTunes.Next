using FoxTunes.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace FoxTunes
{
    public class MouseCursorAdorner : FrameworkElement
    {
        public static readonly ThemeLoader ThemeLoader = ComponentRegistry.Instance.GetComponent<ThemeLoader>();

        public static readonly DependencyProperty TemplateProperty = DependencyProperty.Register(
            "Template",
            typeof(DataTemplate),
            typeof(MouseCursorAdorner),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnTemplatePropertyChanged))
        );

        public static DataTemplate GetTemplate(MouseCursorAdorner owner)
        {
            return (DataTemplate)owner.GetValue(TemplateProperty);
        }

        public static void SetTemplate(MouseCursorAdorner owner, DataTemplate value)
        {
            owner.SetValue(TemplateProperty, value);
        }

        private static void OnTemplatePropertyChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
        {
            var adorner = owner as MouseCursorAdorner;
            if (adorner == null)
            {
                return;
            }
            adorner.OnTemplateChanged();
        }

        public MouseCursorAdorner()
        {
            if (ThemeLoader != null)
            {
                ThemeLoader.ThemeChanged += this.OnThemeChanged;
            }
            this.DataContextChanged += this.OnDataContextChanged;
        }

        protected virtual void OnThemeChanged(object sender, EventArgs e)
        {
            this.Adorner = null;
        }

        protected virtual void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Adorner != null)
            {
                this.Adorner.ContentControl.Content = this.DataContext;
            }
        }

        public DataTemplate Template
        {
            get
            {
                return GetTemplate(this);
            }
            set
            {
                SetTemplate(this, value);
            }
        }

        protected virtual void OnTemplateChanged()
        {
            this.Adorner = null;
        }

        private InternalMouseCursorAdorner Adorner { get; set; }

        public ICommand ShowCommand
        {
            get
            {
                return new Command(this.Show);
            }
        }

        public ICommand HideCommand
        {
            get
            {
                return new Command(this.Hide);
            }
        }

        public void Create()
        {
            this.Adorner = new InternalMouseCursorAdorner(this, GetTemplate(this));
            this.Adorner.ContentControl.Content = this.DataContext;
        }

        public void Show()
        {
            if (this.Adorner == null)
            {
                this.Create();
            }
            else if (this.Adorner.Parent != null)
            {
                return;
            }
            AdornerLayer.GetAdornerLayer(this).Add(this.Adorner);
        }

        public void Hide()
        {
            if (this.Adorner == null || this.Adorner.Parent == null)
            {
                return;
            }
            AdornerLayer.GetAdornerLayer(this).Remove(this.Adorner);
        }

        private class InternalMouseCursorAdorner : Adorner
        {
            public InternalMouseCursorAdorner(UIElement adornedElement, DataTemplate template)
                : base(adornedElement)
            {
                this.ContentControl = new ContentControl()
                {
                    ContentTemplate = template
                };
                this.Brush = new VisualBrush(this.ContentControl);
                this.Pen = new Pen();
                this.IsHitTestVisible = false;
            }

            public ContentControl ContentControl { get; private set; }

            public Point MousePosition { get; private set; }

            public Brush Brush { get; private set; }

            public Pen Pen { get; private set; }

            protected override void OnVisualParentChanged(DependencyObject oldParent)
            {
                if (Windows.ActiveWindow != null)
                {
                    if (this.VisualParent != null)
                    {
                        Windows.ActiveWindow.PreviewDragOver += this.OnPreviewDragOver;
                    }
                    else
                    {
                        Windows.ActiveWindow.PreviewDragOver -= this.OnPreviewDragOver;
                    }
                }
                base.OnVisualParentChanged(oldParent);
            }

            protected virtual void OnPreviewDragOver(object sender, DragEventArgs e)
            {
                this.MousePosition = e.GetPosition(this.AdornedElement);
                this.InvalidateVisual();
            }

            protected override void OnRender(DrawingContext context)
            {
                context.DrawRectangle(this.Brush, this.Pen, new Rect(this.MousePosition.X, this.MousePosition.Y, this.ContentControl.ActualWidth, this.ContentControl.ActualHeight));
                base.OnRender(context);
            }
        }
    }
}
