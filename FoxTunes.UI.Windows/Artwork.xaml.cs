using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Artwork.xaml
    /// </summary>
    [UIComponent("66C8A9E7-0891-48DD-8086-E40F72D4D030", role: UIComponentRole.Info)]
    public partial class Artwork : SquareConfigurableUIComponentBase, IDisposable
    {
        const int TIMEOUT = 100;

        const string CATEGORY = "B40EB21F-0690-4ED0-A628-DECC908E92D0";

        public Artwork()
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
            var viewModel = this.FindResource<global::FoxTunes.ViewModel.Artwork>("ViewModel");
            if (viewModel != null)
            {
                this.IsComponentEnabled = !string.IsNullOrEmpty(viewModel.FileName) && File.Exists(viewModel.FileName);
            }
        }

        protected virtual Task Refresh()
        {
            return Windows.Invoke(() =>
            {
                var viewModel = this.FindResource<global::FoxTunes.ViewModel.Artwork>("ViewModel");
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

        protected virtual void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }
            var viewModel = this.FindResource<global::FoxTunes.ViewModel.Artwork>("ViewModel");
            if (viewModel != null)
            {
                var task = viewModel.Next();
            }
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
                Strings.Artwork_Name,
                ArtworkConfiguration.SECTION
            );
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return ArtworkConfiguration.GetConfigurationSections();
        }
    }
}
