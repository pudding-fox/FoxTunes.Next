using System.Windows.Controls;
using System.Windows.Input;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Info.xaml
    /// </summary>
    [UIComponent("5D6C1128-B84F-4748-941D-C5DE341B6C49", role: UIComponentRole.Info)]
    public partial class Info : UIComponentBase
    {
        public Info()
        {
            this.InitializeComponent();
        }

        protected virtual void OnSearch(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                var viewModel = this.FindResource<global::FoxTunes.ViewModel.Info>("ViewModel");
                if (viewModel == null)
                {
                    return;
                }
                viewModel.Search(textBlock.Name, textBlock.Text);
            }
        }
    }
}
