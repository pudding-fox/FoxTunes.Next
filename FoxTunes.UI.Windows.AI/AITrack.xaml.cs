using FoxTunes.Interfaces;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for AITrack.xaml
    /// </summary>
    [UIComponent("16C461DB-D54F-4892-892E-2042D825CFA9", role: UIComponentRole.Info)]
    public partial class AITrack : ConfigurableUIComponentBase, IDisposable
    {
        public const string CATEGORY = "4FDADDF7-289E-4E5B-A534-5A97A1DF0F68";

        static AITrack()
        {
            Loader.Load("WebView2Loader.dll");
        }

        public AITrack()
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
            var viewModel = default(global::FoxTunes.ViewModel.AITrack);
            if (this.TryFindResource<global::FoxTunes.ViewModel.AITrack>("ViewModel", out viewModel))
            {
                if (!string.IsNullOrEmpty(viewModel.Content))
                {
                    this.WebView2.NavigateToString(viewModel.Content);
                }
                else
                {
                    this.WebView2.NavigateToString("<html></html>");
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
                var viewModel = default(global::FoxTunes.ViewModel.AITrack);
                if (this.TryFindResource<global::FoxTunes.ViewModel.AITrack>("ViewModel", out viewModel))
                {
                    viewModel.Refresh();
                }
                this.WebView2.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Light;
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
                Strings.AITrack_Name,
                AITrackConfiguration.SECTION
            );
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return AITrackConfiguration.GetConfigurationSections();
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
