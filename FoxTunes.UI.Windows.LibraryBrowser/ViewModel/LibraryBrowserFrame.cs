using System;
using System.Linq;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class LibraryBrowserFrame : ViewModelBase
    {
        private LibraryBrowserFrame()
        {

        }

        public LibraryBrowserFrame(LibraryHierarchyNode itemsSource, LibraryHierarchyNodeCollection items) : this()
        {
            this.ItemsSource = itemsSource;
            this.Items = items;
            if (LibraryHierarchyNode.Empty.Equals(itemsSource))
            {
                this.AllItems = this.Items;
            }
            else
            {
                this.AllItems = new LibraryHierarchyNodeCollection(new[]
                {
                    LibraryHierarchyNode.Empty
                }.Concat(this.Items));
            }
            if (this.CanFreeze)
            {
                this.Freeze();
            }
        }

        public LibraryHierarchyNode ItemsSource { get; private set; }

        public LibraryHierarchyNodeCollection Items { get; private set; }

        public LibraryHierarchyNodeCollection AllItems { get; private set; }

        protected override Freezable CreateInstanceCore()
        {
            return new LibraryBrowserFrame();
        }
    }
}
