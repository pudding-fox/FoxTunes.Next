using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Artist.xaml
    /// </summary>
    [UIComponent("68DB40A1-A575-4A5D-9ECC-9A3E2C5EC976", role: UIComponentRole.Info)]
    public partial class Artist : ConfigurableUIComponentBase, IDisposable
    {
        public const string CATEGORY = "27005BD4-2D8E-4E1A-B0F7-E32A415C6DD7";

        public Artist()
        {
            this.InitializeComponent();
            var task = this.WebView2.EnsureCoreWebView2Async();
        }

        protected virtual void OnContentChanged(object sender, EventArgs e)
        {
            var viewModel = default(global::FoxTunes.ViewModel.AIArtist);
            if (this.TryFindResource<global::FoxTunes.ViewModel.AIArtist>("ViewModel", out viewModel))
            {
                if (!string.IsNullOrEmpty(viewModel.Content))
                {
                    this.WebView2.NavigateToString(viewModel.Content);
                }
            }
        }

        protected virtual void OnCoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            this.WebView2.CoreWebView2.ContextMenuRequested += this.OnContextMenuRequested;
        }

        protected virtual void OnContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            if (this.ContextMenu != null)
            {
                this.ContextMenu.IsOpen = true;
            }
            e.Handled = true;
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
                AIArtistConfiguration.SECTION
            );
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return AIArtistConfiguration.GetConfigurationSections();
        }

        protected override void OnDisposing()
        {
            if (this.WebView2 != null && this.WebView2.CoreWebView2 != null)
            {
                this.WebView2.CoreWebView2.ContextMenuRequested -= this.OnContextMenuRequested;
            }
            base.OnDisposing();
        }
    }
}
