using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public class LibraryBrowserTemplateSelector : DataTemplateSelector
    {
        public DataTemplate UpTemplate { get; set; }

        public DataTemplate DefaultTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var libraryHierarchyNode = item as LibraryHierarchyNode;
            if (libraryHierarchyNode != null)
            {
                if (LibraryHierarchyNode.Empty.Equals(libraryHierarchyNode))
                {
                    return this.UpTemplate;
                }
                return this.DefaultTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}
