using FoxTunes.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class UIComponentFactory : StandardFactory
    {
        public FrameworkElement CreateControl(UIComponentConfiguration configuration, out UIComponentBase component)
        {
            if (configuration.Component.IsEmpty)
            {
                component = null;
                return null;
            }
            if (configuration.Component.Type == LayoutManager.PLACEHOLDER)
            {
                //A plugin was uninstalled.
                component = null;
                return null;
            }
            component = ComponentActivator.Instance.Activate<UIComponentBase>(configuration.Component.Type);
            if (component is IUIComponentPanel panel)
            {
                panel.Configuration = configuration;
            }
            //Some components expect to be hosted in a Grid (Artwork..)
            //We might as well add a Rectangle to make the entire thing hit testable.
            var grid = new Grid();
            grid.Children.Add(new Rectangle()
            {
                Fill = Brushes.Transparent
            });
            grid.Children.Add(component);
            return grid;
        }
    }
}
