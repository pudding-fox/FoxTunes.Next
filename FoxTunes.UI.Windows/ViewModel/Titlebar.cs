using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Titlebar : ViewModelBase
    {
        public static readonly DependencyProperty WindowProperty = DependencyProperty.Register(
            "Window",
            typeof(Window),
            typeof(Titlebar),
            new PropertyMetadata(new PropertyChangedCallback(OnWindowChanged))
        );

        public static Window GetWindow(Titlebar source)
        {
            return (Window)source.GetValue(WindowProperty);
        }

        public static void SetWindow(Titlebar source, Window value)
        {
            source.SetValue(WindowProperty, value);
        }

        public static void OnWindowChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var titlebar = sender as Titlebar;
            if (titlebar == null)
            {
                return;
            }
            titlebar.OnWindowChanged();
        }

        public Window Window
        {
            get
            {
                return this.GetValue(WindowProperty) as Window;
            }
            set
            {
                this.SetValue(WindowProperty, value);
            }
        }

        protected virtual void OnWindowChanged()
        {
            if (this.Window != null)
            {
                DependencyPropertyDescriptor.FromProperty(
                    Window.TitleProperty,
                    typeof(Window)
                ).AddValueChanged(this.Window, (sender, e) => this.OnTitleChanged());
                DependencyPropertyDescriptor.FromProperty(
                    Window.WindowStateProperty,
                    typeof(Window)
                ).AddValueChanged(this.Window, (sender, e) => this.OnWindowStateChanged());
            }
            this.OnTitleChanged();
            this.OnWindowStateChanged();
            this.OnCanMaximizeRestoreChanged();
            if (this.WindowChanged != null)
            {
                this.WindowChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Window");
        }

        public event EventHandler WindowChanged;

        public Stream Icon
        {
            get
            {
                return typeof(Titlebar).Assembly.GetManifestResourceStream("FoxTunes.UI.Windows.Images.Fox.ico");
            }
        }

        public string Title
        {
            get
            {
                if (this.Window != null)
                {
                    return this.Window.Title;
                }
                return null;
            }
            set
            {
                if (this.Window != null)
                {
                    this.Window.Title = value;
                }
                this.OnTitleChanged();
            }
        }

        protected virtual void OnTitleChanged()
        {
            if (this.TitleChanged != null)
            {
                this.TitleChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Title");
        }

        public event EventHandler TitleChanged;

        public global::System.Windows.WindowState WindowState
        {
            get
            {
                if (this.Window != null)
                {
                    return this.Window.WindowState;
                }
                return global::System.Windows.WindowState.Normal;
            }
            set
            {
                if (this.Window != null)
                {
                    this.Window.WindowState = value;
                }
                this.OnWindowStateChanged();
            }
        }

        protected virtual void OnWindowStateChanged()
        {
            if (this.WindowStateChanged != null)
            {
                this.WindowStateChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("WindowState");
        }

        public event EventHandler WindowStateChanged;

        public ICommand MinimizeCommand
        {
            get
            {
                return new Command(this.Minimize);
            }
        }

        public void Minimize()
        {
            this.WindowState = global::System.Windows.WindowState.Minimized;
        }

        public ICommand MaximizeRestoreCommand
        {
            get
            {
                return new Command(this.MaximizeRestore, () => this.CanMaximizeRestore);
            }
        }

        public void MaximizeRestore()
        {
            switch (this.WindowState)
            {
                case global::System.Windows.WindowState.Normal:
                    this.WindowState = global::System.Windows.WindowState.Maximized;
                    break;
                case global::System.Windows.WindowState.Maximized:
                    this.WindowState = global::System.Windows.WindowState.Normal;
                    break;
            }
        }

        public bool CanMaximizeRestore
        {
            get
            {
                if (this.Window != null)
                {
                    return this.Window.ResizeMode != ResizeMode.NoResize;
                }
                return true;
            }
        }

        protected virtual void OnCanMaximizeRestoreChanged()
        {
            if (this.CanMaximizeRestoreChanged != null)
            {
                this.CanMaximizeRestoreChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CanMaximizeRestore");
        }

        public event EventHandler CanMaximizeRestoreChanged;

        public ICommand CloseCommand
        {
            get
            {
                return new Command(this.Close);
            }
        }

        public void Close()
        {
            if (this.Window != null)
            {
                this.Window.Close();
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Titlebar();
        }
    }
}
