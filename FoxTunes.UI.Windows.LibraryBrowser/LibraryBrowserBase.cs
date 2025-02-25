using FoxDb;
using FoxTunes.Interfaces;
using FoxTunes.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace FoxTunes
{
    public abstract class LibraryBrowserBase : ConfigurableUIComponentBase
    {
        protected override void CreateMenu()
        {
            var menu = new Menu()
            {
                Components = new ObservableCollection<IInvocableComponent>(
                    ComponentRegistry.Instance.GetComponents<IInvocableComponent>().Concat(new[] { this })
                ),
                Category = InvocationComponent.CATEGORY_LIBRARY
            };
            this.ContextMenu = menu;
        }

        public IntegerConfigurationElement TileSize { get; private set; }

        public SelectionConfigurationElement ImageMode { get; private set; }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.TileSize = this.Configuration.GetElement<IntegerConfigurationElement>(
                     LibraryBrowserBaseConfiguration.SECTION,
                     LibraryBrowserBaseConfiguration.TILE_SIZE
                 );
                this.ImageMode = this.Configuration.GetElement<SelectionConfigurationElement>(
                    LibraryBrowserBaseConfiguration.SECTION,
                    LibraryBrowserBaseConfiguration.TILE_IMAGE
                );
                this.TileSize.ValueChanged += this.OnValueChanged;
                this.ImageMode.ValueChanged += this.OnValueChanged;
            }
            base.OnConfigurationChanged();
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            //It's important that we perform the refresh with low priority, we need to wait for bindings to settle.
            var task = Windows.Invoke(this.RefreshVisibleItems, DispatcherPriority.ApplicationIdle);
        }

        protected virtual void RefreshVisibleItems()
        {
            var listBox = this.GetActiveListBox();
            if (listBox == null)
            {
                return;
            }
            var names = new[]
            {
                CommonImageTypes.FrontCover
            };
            var listBoxItems = listBox.FindChildren<ListBoxItem>();
            foreach (var listBoxItem in listBoxItems)
            {
                if (listBoxItem.Content is LibraryHierarchyNode libraryHierarchyNode)
                {
                    libraryHierarchyNode.Refresh(names);
                }
            }
        }

        protected abstract ItemsControl GetItemsControl();

        protected abstract MouseCursorAdorner GetMouseCursorAdorner();

        public ListBox GetActiveListBox()
        {
            var itemsControl = this.GetItemsControl();
            if (itemsControl == null)
            {
                return null;
            }
            var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(itemsControl.Items.Count - 1) as ContentPresenter;
            if (container == null)
            {
                return null;
            }
            container.ApplyTemplate();
            var listBox = container.ContentTemplate.FindName("ListBox", container) as ListBox;
            return listBox;
        }

        public IEnumerable<ListBox> GetInactiveListBoxes()
        {
            var itemsControl = this.GetItemsControl();
            if (itemsControl != null)
            {
                for (var a = 0; a < itemsControl.Items.Count - 1; a++)
                {
                    var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(a) as ContentPresenter;
                    if (container != null)
                    {
                        container.ApplyTemplate();
                        var listBox = container.ContentTemplate.FindName("ListBox", container) as ListBox;
                        yield return listBox;
                    }
                }
            }
        }

        protected virtual void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null || listBox.SelectedItem == null)
            {
                return;
            }
            listBox.ScrollIntoView(listBox.SelectedItem);
        }

        protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null || listBox.SelectedItem == null)
            {
                return;
            }
            listBox.ScrollIntoView(listBox.SelectedItem);
        }

        protected virtual void OnListBoxLoaded(object sender, RoutedEventArgs e)
        {
            this.ApplyBlur();
            this.FixFocus();
        }

        protected virtual void OnListBoxUnloaded(object sender, RoutedEventArgs e)
        {
            this.ApplyBlur();
            this.FixFocus();
        }

        protected virtual void FixFocus()
        {
            var itemsControl = this.GetItemsControl();
            if (itemsControl == null)
            {
                return;
            }
            if (!itemsControl.IsKeyboardFocusWithin)
            {
                return;
            }
            Keyboard.ClearFocus();
            var listBox = this.GetActiveListBox();
            if (listBox == null)
            {
                return;
            }
            var index = listBox.SelectedIndex;
            if (index == -1)
            {
                index = 1;
            }
            var container = listBox.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
            if (container == null)
            {
                return;
            }
            Keyboard.Focus(container);
        }

        protected virtual void ApplyBlur()
        {
            var activeListBox = this.GetActiveListBox();
            var inactiveListBoxes = this.GetInactiveListBoxes();
            if (activeListBox != null)
            {
                UIElementExtensions.SetTransparentBlur(activeListBox, false);
            }
            foreach (var inactiveListBox in inactiveListBoxes)
            {
                UIElementExtensions.SetTransparentBlur(inactiveListBox, true);
            }
        }

        protected virtual void DragSourceInitialized(object sender, ListBoxExtensions.DragSourceInitializedEventArgs e)
        {
            var mouseCursorAdorner = this.GetMouseCursorAdorner();
            if (mouseCursorAdorner == null)
            {
                return;
            }
            var viewModel = this.FindResource<global::FoxTunes.ViewModel.LibraryBrowser>("ViewModel");
            if (viewModel == null || viewModel.SelectedItem == null)
            {
                return;
            }
            //Only show adorners when hosted in main window.
            if (this.IsHostedIn<MainWindow>() && viewModel.ShowCursorAdorners)
            {
                mouseCursorAdorner.Show();
            }
            try
            {
                DragDrop.DoDragDrop(
                    this,
                    e.Data,
                    DragDropEffects.Copy
                );
            }
            finally
            {
                if (mouseCursorAdorner.IsVisible)
                {
                    mouseCursorAdorner.Hide();
                }
            }
        }

        public override IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_LIBRARY;
            }
        }

        public override IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_LIBRARY,
                     this.TileSize.Id,
                     Strings.LibraryBrowserBase_Size_Small,
                     path: string.Concat(Strings.LibraryBrowserBase_Path, Path.AltDirectorySeparatorChar, this.TileSize.Name),
                     attributes: this.TileSize.Value == LibraryBrowserBaseConfiguration.TILE_SIZE_SMALL ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_LIBRARY,
                     this.TileSize.Id,
                     Strings.LibraryBrowserBase_Size_Medium,
                     path: string.Concat(Strings.LibraryBrowserBase_Path, Path.AltDirectorySeparatorChar, this.TileSize.Name),
                     attributes: this.TileSize.Value == LibraryBrowserBaseConfiguration.TILE_SIZE_MEDIUM ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_LIBRARY,
                     this.TileSize.Id,
                     Strings.LibraryBrowserBase_Size_Large,
                     path: string.Concat(Strings.LibraryBrowserBase_Path, Path.AltDirectorySeparatorChar, this.TileSize.Name),
                     attributes: this.TileSize.Value == LibraryBrowserBaseConfiguration.TILE_SIZE_LARGE ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                foreach (var option in this.ImageMode.Options)
                {
                    yield return new InvocationComponent(
                        InvocationComponent.CATEGORY_LIBRARY,
                        this.ImageMode.Id,
                        option.Name,
                        path: string.Concat(Strings.LibraryBrowserBase_Path, Path.AltDirectorySeparatorChar, this.ImageMode.Name),
                        attributes: this.ImageMode.Value == option ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                    );
                }
                yield return new InvocationComponent(
                     InvocationComponent.CATEGORY_LIBRARY,
                    SETTINGS,
                    StringResources.General_Settings,
                    path: Strings.LibraryBrowserBase_Path,
                    attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                );
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            if (string.Equals(component.Id, this.TileSize.Id, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(component.Name, Strings.LibraryBrowserBase_Size_Small, StringComparison.OrdinalIgnoreCase))
                {
                    this.TileSize.Value = LibraryBrowserBaseConfiguration.TILE_SIZE_SMALL;
                }
                else if (string.Equals(component.Name, Strings.LibraryBrowserBase_Size_Medium, StringComparison.OrdinalIgnoreCase))
                {
                    this.TileSize.Value = LibraryBrowserBaseConfiguration.TILE_SIZE_MEDIUM;
                }
                else if (string.Equals(component.Name, Strings.LibraryBrowserBase_Size_Large, StringComparison.OrdinalIgnoreCase))
                {
                    this.TileSize.Value = LibraryBrowserBaseConfiguration.TILE_SIZE_LARGE;
                }
            }
            else if (string.Equals(component.Id, this.ImageMode.Id, StringComparison.OrdinalIgnoreCase))
            {
                var mode = this.ImageMode.Options.FirstOrDefault(option => string.Equals(option.Name, component.Name, StringComparison.OrdinalIgnoreCase));
                if (mode != null)
                {
                    this.ImageMode.Value = mode;
                }
            }
            else if (string.Equals(component.Id, SETTINGS, StringComparison.OrdinalIgnoreCase))
            {
                return this.ShowSettings();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected override Task<bool> ShowSettings()
        {
            return this.ShowSettings(
                Strings.LibraryBrowserBaseConfiguration_Section,
                LibraryBrowserBaseConfiguration.SECTION
            );
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return LibraryBrowserBaseConfiguration.GetConfigurationSections();
        }
    }
}
