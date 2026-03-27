using FoxTunes.Interfaces;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for AIArtist.xaml
    /// </summary>
    [UIComponent("68DB40A1-A575-4A5D-9ECC-9A3E2C5EC976", role: UIComponentRole.Info)]
    public partial class AIArtist : ConfigurableUIComponentBase, IDisposable
    {
        public const string CATEGORY = "27005BD4-2D8E-4E1A-B0F7-E32A415C6DD7";

        static AIArtist()
        {
            Loader.Load("WebView2Loader.dll");
        }

        public AIArtist()
        {
            this.InitializeComponent();
            var task = this.EnsureCoreWebView2Async();
        }

        protected virtual async Task EnsureCoreWebView2Async()
        {
            var options = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = "--force-dark-mode=0"
            };
            var env = await CoreWebView2Environment.CreateAsync(null, null, options).ConfigureAwait(false);
            await Windows.Invoke(() => this.WebView2.EnsureCoreWebView2Async(env)).ConfigureAwait(false);
        }

        protected override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
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
            if (!e.IsSuccess)
            {
                return;
            }
            var task = Windows.Invoke(() =>
            {
                var viewModel = default(global::FoxTunes.ViewModel.AIArtist);
                if (this.TryFindResource<global::FoxTunes.ViewModel.AIArtist>("ViewModel", out viewModel))
                {
                    viewModel.Refresh();
                }
                this.WebView2.CoreWebView2.ContextMenuRequested += this.OnContextMenuRequested;
            });
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
                Strings.AIArtist_Name,
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
