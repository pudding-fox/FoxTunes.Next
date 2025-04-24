using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Artist.xaml
    /// </summary>
    [UIComponent("9ECF7D0C-1CBC-4529-9C46-1DB0C0B28771", role: UIComponentRole.Info)]
    public partial class Artist : ConfigurableUIComponentBase, IDisposable
    {
        const int TIMEOUT = 100;

        const string CATEGORY = "B40EB21F-0690-4ED0-A628-DECC908E92D0";

        public Artist()
        {
            this.Debouncer = new AsyncDebouncer(TIMEOUT);
            this.InitializeComponent();
            this.OnFileNameChanged(this, EventArgs.Empty);
        }

        public AsyncDebouncer Debouncer { get; private set; }

        protected virtual void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Debouncer.Exec(this.Refresh);
        }

        protected virtual void OnFileNameChanged(object sender, EventArgs e)
        {
            var viewModel = this.FindResource<global::FoxTunes.ViewModel.Artist>("ViewModel");
            if (viewModel != null)
            {
                this.IsComponentEnabled = !string.IsNullOrEmpty(viewModel.FileName) && File.Exists(viewModel.FileName);
            }
        }

        protected virtual Task Refresh()
        {
            return Windows.Invoke(() =>
            {
                var viewModel = this.FindResource<global::FoxTunes.ViewModel.Artist>("ViewModel");
                if (viewModel != null)
                {
                    viewModel.Emit();
                }
            });
        }

        protected override void OnDisposing()
        {
            if (this.Debouncer != null)
            {
                this.Debouncer.Dispose();
            }
            base.OnDisposing();
        }


        public override IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return CATEGORY;
            }
        }

        protected override Task<bool> ShowSettings()
        {
            return this.ShowSettings(
                Strings.Artist_Name,
                ArtistConfiguration.SECTION
            );
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return ArtistConfiguration.GetConfigurationSections();
        }
    }
}
