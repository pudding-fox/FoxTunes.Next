using FoxTunes.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for MetaDataEditor.xaml
    /// </summary>
    public partial class MetaDataEditor : UserControl
    {
        public MetaDataEditor()
        {
            this.InitializeComponent();
        }

        public void Edit(IFileData[] fileDatas)
        {
            var viewModel = this.FindResource<global::FoxTunes.ViewModel.MetaDataEditor>("ViewModel");
            if (viewModel != null)
            {
                viewModel.Edit(fileDatas);
            }
        }

        protected virtual void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement frameworkElement)
            {
                var metaDataEntry = frameworkElement.FindDataContext<global::FoxTunes.ViewModel.MetaDataEntry>();
                if (metaDataEntry == null || !metaDataEntry.BrowseCommand.CanExecute(null))
                {
                    return;
                }
                metaDataEntry.BrowseCommand.Execute(null);
            }
        }
    }
}
