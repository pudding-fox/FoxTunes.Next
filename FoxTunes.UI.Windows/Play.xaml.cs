using System;
using System.Windows;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Play.xaml
    /// </summary>
    [UIComponent("705CFB8A-D704-4D12-B8B6-7576EC431F33", role: UIComponentRole.Playback)]
    public partial class Play : UIComponentBase
    {
        public Play()
        {
            this.InitializeComponent();
            this.OnIsPlayingChanged(this, EventArgs.Empty);
        }

        protected virtual void OnIsPlayingChanged(object sender, EventArgs e)
        {
            var viewModel = default(global::FoxTunes.ViewModel.Play);
            if (this.TryFindResource<global::FoxTunes.ViewModel.Play>("ViewModel", out viewModel))
            {
                if (viewModel.IsPlaying)
                {
                    var content = default(FrameworkElement);
                    if (this.TryFindResource<FrameworkElement>("PauseContent", out content))
                    {
                        this.Button.Content = content;
                    }
                }
                else
                {
                    var content = default(FrameworkElement);
                    if (this.TryFindResource<FrameworkElement>("PlayContent", out content))
                    {
                        this.Button.Content = content;
                    }
                }
            }
        }
    }
}
