using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FoxTunes
{
    public abstract class SquareUIComponentBase : UIComponentBase
    {
        public static readonly DependencyProperty SizeModeProperty = DependencyProperty.Register(
            "SizeMode",
            typeof(SquareUIComponentSizeMode),
            typeof(SquareUIComponentBase),
            new FrameworkPropertyMetadata(SquareUIComponentSizeMode.ToHeight, new PropertyChangedCallback(OnSizeModeChanged))
        );

        public static SquareUIComponentSizeMode GetSizeMode(SquareUIComponentBase source)
        {
            return (SquareUIComponentSizeMode)source.GetValue(SizeModeProperty);
        }

        public static void SetSizeMode(SquareUIComponentBase source, SquareUIComponentSizeMode value)
        {
            source.SetValue(SizeModeProperty, value);
        }

        public static void OnSizeModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var squareUIComponentBase = sender as SquareUIComponentBase;
            if (squareUIComponentBase == null)
            {
                return;
            }
            squareUIComponentBase.OnSizeModeChanged();
        }

        public SquareUIComponentSizeMode SizeMode
        {
            get
            {
                return GetSizeMode(this);
            }
            set
            {
                SetSizeMode(this, value);
            }
        }

        protected virtual void OnSizeModeChanged()
        {
            this.UpdateLayoutSource();
            if (this.SizeModeChanged != null)
            {
                this.SizeModeChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler SizeModeChanged;

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            this.UpdateLayoutSource();
            base.OnVisualParentChanged(oldParent);
        }

        protected virtual void UpdateLayoutSource()
        {
            if (this.Parent == null)
            {
                return;
            }
            var grid = this.Parent.FindAncestor<Grid>();
            if (grid == null)
            {
                return;
            }
            var index = default(int);
            switch (this.SizeMode)
            {
                case SquareUIComponentSizeMode.ToWidth:
                    var column = default(ColumnDefinition);
                    index = Grid.GetColumn(this);
                    if (index < grid.ColumnDefinitions.Count)
                    {
                        column = grid.ColumnDefinitions[index];
                        BindingHelper.AddHandler(column, ColumnDefinition.WidthProperty, typeof(ColumnDefinition), (sender, e) =>
                        {
                            this.UpdateLayoutSource(column);
                        });
                    }
                    this.UpdateLayoutSource(column);
                    break;
                case SquareUIComponentSizeMode.ToHeight:
                    var row = default(RowDefinition);
                    index = Grid.GetRow(this);
                    if (index < grid.RowDefinitions.Count)
                    {
                        row = grid.RowDefinitions[index];
                        BindingHelper.AddHandler(row, RowDefinition.HeightProperty, typeof(RowDefinition), (sender, e) =>
                        {
                            this.UpdateLayoutSource(row);
                        });
                    }
                    this.UpdateLayoutSource(row);
                    break;
            }
        }

        protected virtual void UpdateLayoutSource(ColumnDefinition column = null)
        {
            if (column == null || column.Width.IsAuto || column.Width.IsStar)
            {
                this.SizeChanged += this.OnSizeChanged;
            }
            else
            {
                BindingOperations.ClearBinding(this, WidthProperty);
                BindingOperations.ClearBinding(this, HeightProperty);
            }
        }


        protected virtual void UpdateLayoutSource(RowDefinition row = null)
        {
            if (row == null || row.Height.IsAuto || row.Height.IsStar)
            {
                this.SizeChanged += this.OnSizeChanged;
            }
            else
            {
                BindingOperations.ClearBinding(this, WidthProperty);
                BindingOperations.ClearBinding(this, HeightProperty);
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            BindingOperations.ClearBinding(this, WidthProperty);
            BindingOperations.ClearBinding(this, HeightProperty);
            switch (this.SizeMode)
            {
                case SquareUIComponentSizeMode.ToWidth:
                    if (!this.SizeToWidth() && !this.SizeToHeight())
                    {
                        return;
                    }
                    break;
                case SquareUIComponentSizeMode.ToHeight:
                    if (!this.SizeToHeight() && !this.SizeToWidth())
                    {
                        return;
                    }
                    break;
            }
            this.SizeChanged -= this.OnSizeChanged;
        }

        protected virtual bool SizeToWidth()
        {
            if (this.ActualWidth > 0)
            {
                BindingOperations.SetBinding(this, HeightProperty, new Binding("ActualWidth")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.Self)
                });
                return true;
            }
            return false;
        }

        protected virtual bool SizeToHeight()
        {
            if (this.ActualHeight > 0)
            {
                BindingOperations.SetBinding(this, WidthProperty, new Binding("ActualHeight")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.Self)
                });
                return true;
            }
            return false;
        }
    }

    public enum SquareUIComponentSizeMode : byte
    {
        None,
        ToWidth,
        ToHeight
    }
}
