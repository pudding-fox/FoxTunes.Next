using FoxTunes.Interfaces;
using System;
using System.Drawing.Design;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for ToolWindowManager.xaml
    /// </summary>
    public partial class ToolWindowManager : UserControl
    {
        public static readonly IConfiguration Configuration = ComponentRegistry.Instance.GetComponent<IConfiguration>();

        public ToolWindowManager()
        {
            this.InitializeComponent();
        }

        protected virtual async void OnEditLayoutXMLClick(object sender, RoutedEventArgs e)
        {
            var viewModel = this.TryFindResource("ViewModel") as global::FoxTunes.ViewModel.ToolWindowManager;
            if (viewModel == null)
            {
                return;
            }
            var toolWindow = viewModel.SelectedWindow;
            if (toolWindow == null)
            {
                return;
            }
            //Ensure any pending changes are committed.
            Configuration.Save();
            var layoutEditor = new LayoutEditor();
            layoutEditor.Component = toolWindow.Component;
            if (!await Windows.ShowDialog(Core.Instance, Strings.ToolWindowManager_EditLayoutXML, layoutEditor).ConfigureAwait(false))
            {
                return;
            }
            await Windows.Invoke(() => toolWindow.Component = layoutEditor.Component).ConfigureAwait(false);
        }

        protected virtual void OnCopyLayoutToMainClick(object sender, RoutedEventArgs e)
        {
            var viewModel = this.TryFindResource("ViewModel") as global::FoxTunes.ViewModel.ToolWindowManager;
            if (viewModel == null)
            {
                return;
            }
            var toolWindow = viewModel.SelectedWindow;
            if (toolWindow == null)
            {
                return;
            }
            var mainLayout = Core.Instance.Components.Configuration.GetElement<TextConfigurationElement>(
                UIComponentLayoutProviderConfiguration.SECTION,
                UIComponentLayoutProviderConfiguration.MAIN_LAYOUT
            );
            if (mainLayout == null)
            {
                return;
            }
            //Ensure any pending changes are committed.
            Configuration.Save();
            var converter = new global::FoxTunes.ViewModel.UIComponentConfigurationConverter();
            mainLayout.Value = string.Concat(
                "<?xml version=\"1.0\" encoding=\"Windows-1252\"?>\r\n<FoxTunes>\r\n",
                Convert.ToString(
                    converter.Convert(toolWindow.Component, typeof(string), null, CultureInfo.CurrentCulture)
                ),
                "\r\n</FoxTunes>"
            );
        }
    }
}
