using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Components.xaml
    /// </summary>
    public partial class Components : UserControl
    {
        public Components()
        {
            this.InitializeComponent();
        }

        protected virtual void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = this.FindResource<global::FoxTunes.ViewModel.Components>("ViewModel");
            if (viewModel == null)
            {
                return;
            }
            viewModel.Enabled = this.IsVisible;
        }
    }
}
