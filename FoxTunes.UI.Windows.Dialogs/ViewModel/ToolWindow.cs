using System;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class ToolWindow : ViewModelBase
    {
        public string Title
        {
            get
            {
                if (this.Configuration == null)
                {
                    return string.Empty;
                }
                return ToolWindowConfiguration.GetTitle(this.Configuration);
            }
            set
            {
                if (this.Configuration == null || string.Equals(this.Configuration.Title, value))
                {
                    return;
                }
                this.Configuration.Title = value;
            }
        }

        protected virtual void OnTitleChanged(object sender, EventArgs e)
        {
            if (this.TitleChanged != null)
            {
                this.TitleChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Title");
        }

        public event EventHandler TitleChanged;

        public Rect Bounds
        {
            get
            {
                if (this.Configuration == null)
                {
                    return Rect.Empty;
                }
                return new Rect(this.Configuration.Left, this.Configuration.Top, this.Configuration.Width, this.Configuration.Height);
            }
            set
            {
                this.Configuration.Left = !double.IsNaN(value.Left) ? Convert.ToInt32(value.Left) : 0;
                this.Configuration.Top = !double.IsNaN(value.Top) ? Convert.ToInt32(value.Top) : 0;
                this.Configuration.Width = !double.IsNaN(value.Width) ? Convert.ToInt32(value.Width) : 0;
                this.Configuration.Height = !double.IsNaN(value.Height) ? Convert.ToInt32(value.Height) : 0;
                this.OnBoundsChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void OnBoundsChanged(object sender, EventArgs e)
        {
            if (this.BoundsChanged != null)
            {
                this.BoundsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Bounds");
        }

        public event EventHandler BoundsChanged;

        public UIComponentConfiguration Component
        {
            get
            {
                if (this.Configuration == null)
                {
                    return null;
                }
                return this.Configuration.Component;
            }
            set
            {
                if (this.Configuration == null || object.ReferenceEquals(this.Configuration.Component, value))
                {
                    return;
                }
                this.Configuration.Component = value;
            }
        }

        protected virtual void OnComponentChanged(object sender, EventArgs e)
        {
            this.OnTitleChanged(this, EventArgs.Empty);
            if (this.ComponentChanged != null)
            {
                this.ComponentChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Component");
        }

        public event EventHandler ComponentChanged;

        public bool ShowWithMainWindow
        {
            get
            {
                if (this.Configuration == null)
                {
                    return false;
                }
                return this.Configuration.ShowWithMainWindow;
            }
            set
            {
                if (this.Configuration == null || this.Configuration.ShowWithMainWindow == value)
                {
                    return;
                }
                this.Configuration.ShowWithMainWindow = value;
            }
        }

        protected virtual void OnShowWithMainWindowChanged(object sender, EventArgs e)
        {
            if (this.ShowWithMainWindowChanged != null)
            {
                this.ShowWithMainWindowChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowWithMainWindow");
        }

        public event EventHandler ShowWithMainWindowChanged;

        public bool ShowWithMiniWindow
        {
            get
            {
                if (this.Configuration == null)
                {
                    return false;
                }
                return this.Configuration.ShowWithMiniWindow;
            }
            set
            {
                if (this.Configuration == null || this.Configuration.ShowWithMiniWindow == value)
                {
                    return;
                }
                this.Configuration.ShowWithMiniWindow = value;
            }
        }

        protected virtual void OnShowWithMiniWindowChanged(object sender, EventArgs e)
        {
            if (this.ShowWithMiniWindowChanged != null)
            {
                this.ShowWithMiniWindowChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowWithMiniWindow");
        }

        public event EventHandler ShowWithMiniWindowChanged;

        public bool AlwaysOnTop
        {
            get
            {
                if (this.Configuration == null)
                {
                    return false;
                }
                return this.Configuration.AlwaysOnTop;
            }
            set
            {
                if (this.Configuration == null || this.Configuration.AlwaysOnTop == value)
                {
                    return;
                }
                this.Configuration.AlwaysOnTop = value;
            }
        }

        protected virtual void OnAlwaysOnTopChanged(object sender, EventArgs e)
        {
            if (this.AlwaysOnTopChanged != null)
            {
                this.AlwaysOnTopChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("AlwaysOnTop");
        }

        public event EventHandler AlwaysOnTopChanged;

        private ToolWindowConfiguration _Configuration { get; set; }

        public ToolWindowConfiguration Configuration
        {
            get
            {
                return this._Configuration;
            }
            set
            {
                if (object.ReferenceEquals(this.Configuration, value))
                {
                    return;
                }
                this._Configuration = value;
                this.OnConfigurationChanged();
            }
        }

        protected virtual void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Configuration.TitleChanged += this.OnTitleChanged;
                this.Configuration.ComponentChanged += this.OnComponentChanged;
                this.Configuration.ShowWithMainWindowChanged += this.OnShowWithMainWindowChanged;
                this.Configuration.ShowWithMiniWindowChanged += this.OnShowWithMiniWindowChanged;
                this.Configuration.AlwaysOnTopChanged += this.OnAlwaysOnTopChanged;

                this.OnTitleChanged(this, EventArgs.Empty);
                this.OnBoundsChanged(this, EventArgs.Empty);
                this.OnComponentChanged(this, EventArgs.Empty);
                this.OnShowWithMainWindowChanged(this, EventArgs.Empty);
                this.OnShowWithMiniWindowChanged(this, EventArgs.Empty);
                this.OnAlwaysOnTopChanged(this, EventArgs.Empty);
            }
            if (this.ConfigurationChanged != null)
            {
                this.ConfigurationChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Configuration");
        }

        public event EventHandler ConfigurationChanged;

        protected override Freezable CreateInstanceCore()
        {
            return new ToolWindow();
        }

        protected override void OnDisposing()
        {
            if (this.Configuration != null)
            {
                this.Configuration.TitleChanged -= this.OnTitleChanged;
                this.Configuration.ComponentChanged -= this.OnComponentChanged;
                this.Configuration.ShowWithMainWindowChanged -= this.OnShowWithMainWindowChanged;
                this.Configuration.ShowWithMiniWindowChanged -= this.OnShowWithMiniWindowChanged;
                this.Configuration.AlwaysOnTopChanged -= this.OnAlwaysOnTopChanged;
            }
            base.OnDisposing();
        }
    }
}
