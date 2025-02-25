using System.Windows.Controls;

namespace FoxTunes
{
    public partial class LayoutEditor : UserControl
    {
        public LayoutEditor()
        {
            this.InitializeComponent();
        }

        public UIComponentConfiguration Component
        {
            get
            {
                var viewModel = this.TryFindResource("ViewModel") as global::FoxTunes.ViewModel.LayoutEditor;
                if (viewModel != null)
                {
                    return viewModel.Component;
                }
                return null;
            }
            set
            {
                var viewModel = this.TryFindResource("ViewModel") as global::FoxTunes.ViewModel.LayoutEditor;
                if (viewModel != null)
                {
                    viewModel.Component = value;
                }
            }
        }
    }
}