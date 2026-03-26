using FoxTunes.Interfaces;
using System;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class FirstRun : ViewModelBase
    {
        public IConfiguration Configuration { get; private set; }

        public SelectionConfigurationElement Theme { get; private set; }

        public SelectionConfigurationElement Layout { get; private set; }

        public IUILayoutProvider LayoutProvider
        {
            get
            {
                return LayoutManager.Instance.Provider;
            }
        }

        public BooleanConfigurationElement Transparency { get; private set;}

        public SelectionConfigurationElement TransparencyProvider { get; private set; }

        protected virtual void OnLayoutProviderChanged()
        {
            if (this.LayoutProviderChanged != null)
            {
                this.LayoutProviderChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("LayoutProvider");
        }

        public event EventHandler LayoutProviderChanged;

        protected override void InitializeComponent(ICore core)
        {
            LayoutManager.Instance.ProviderChanged += this.OnProviderChanged;
            this.Configuration = core.Components.Configuration;
            this.Theme = this.Configuration.GetElement<SelectionConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.THEME_ELEMENT
            );
            this.Layout = this.Configuration.GetElement<SelectionConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.LAYOUT_ELEMENT
            );
            this.Transparency = this.Configuration.GetElement<BooleanConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.TRANSPARENCY
            );
            this.TransparencyProvider = this.Configuration.GetElement<SelectionConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.TRANSPARENCY_PROVIDER
            );
            base.InitializeComponent(core);
        }

        protected virtual void OnProviderChanged(object sender, EventArgs e)
        {
            this.OnLayoutProviderChanged();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new FirstRun();
        }
    }
}
