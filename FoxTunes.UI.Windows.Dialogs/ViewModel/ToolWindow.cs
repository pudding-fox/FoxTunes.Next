using System;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class ToolWindow : ViewModelBase
    {
        public ToolWindow() : base(false)
        {

        }

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
                if (this.Configuration == null)
                {
                    return;
                }
                this.Configuration.Left = !double.IsNaN(value.Left) && !double.IsInfinity(value.Left) ? Convert.ToInt32(value.Left) : 0;
                this.Configuration.Top = !double.IsNaN(value.Top) && !double.IsInfinity(value.Top) ? Convert.ToInt32(value.Top) : 0;
                this.Configuration.Width = !double.IsNaN(value.Width) && !double.IsInfinity(value.Width) ? Convert.ToInt32(value.Width) : 0;
                this.Configuration.Height = !double.IsNaN(value.Height) && !double.IsInfinity(value.Height) ? Convert.ToInt32(value.Height) : 0;
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
    }
}
