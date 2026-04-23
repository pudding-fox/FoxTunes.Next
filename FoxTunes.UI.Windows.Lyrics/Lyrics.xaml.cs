using System;
using System.Windows;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Lyrics.xaml
    /// </summary>
    [UIComponent("F7774E81-26FC-4F0C-8E0A-67214D155547", role: UIComponentRole.Info)]
    public partial class Lyrics : UIComponentBase
    {
        public Lyrics()
        {
            this.InitializeComponent();
            this.OnHasPlainDataChanged(this, EventArgs.Empty);
            this.OnHasSyncedDataChanged(this, EventArgs.Empty);
        }

        protected virtual void OnHasPlainDataChanged(object sender, EventArgs e)
        {
            var viewModel = default(global::FoxTunes.ViewModel.Lyrics);
            if (this.TryFindResource<global::FoxTunes.ViewModel.Lyrics>("ViewModel", out viewModel))
            {
                if (viewModel.HasPlainData)
                {
                    var style = default(Style);
                    if (this.TryFindResource<Style>("PlainStyle", out style))
                    {
                        this.ContentControl.Style = style;
                    }
                }
            }
        }

        protected virtual void OnHasSyncedDataChanged(object sender, EventArgs e)
        {
            var viewModel = default(global::FoxTunes.ViewModel.Lyrics);
            if (this.TryFindResource<global::FoxTunes.ViewModel.Lyrics>("ViewModel", out viewModel))
            {
                if (viewModel.HasSyncedData)
                {
                    var style = default(Style);
                    if (this.TryFindResource<Style>("SyncedStyle", out style))
                    {
                        this.ContentControl.Style = style;
                    }
                }
            }
        }
    }
}
