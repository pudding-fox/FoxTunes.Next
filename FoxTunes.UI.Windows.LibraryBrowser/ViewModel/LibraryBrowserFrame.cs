using System;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class LibraryBrowserFrame : ViewModelBase
    {
        private LibraryBrowserFrame()
        {

        }

        public LibraryBrowserFrame(LibraryHierarchyNode itemsSource, LibraryHierarchyNode[] items) : this()
        {
            this.ItemsSource = itemsSource;
            this.Items = items;
            if (LibraryHierarchyNode.Empty.Equals(itemsSource))
            {
                this.AllItems = this.Items;
            }
            else
            {
                this.AllItems = new LibraryHierarchyNode[this.Items.Length + 1];
                this.AllItems[0] = LibraryHierarchyNode.Empty;
                Array.Copy(this.Items, 0, this.AllItems, 1, this.Items.Length);
            }
            if (this.CanFreeze)
            {
                this.Freeze();
            }
        }

        public LibraryHierarchyNode ItemsSource { get; private set; }

        public LibraryHierarchyNode[] Items { get; private set; }

        public LibraryHierarchyNode[] AllItems { get; private set; }

        protected override Freezable CreateInstanceCore()
        {
            return new LibraryBrowserFrame();
        }
    }
}
