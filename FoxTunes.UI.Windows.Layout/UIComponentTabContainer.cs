using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace FoxTunes
{
    [UIComponent("67A0F63C-DC86-4B4E-91E1-290B71822853", children: UIComponentAttribute.UNLIMITED_CHILDREN, role: UIComponentRole.Container)]
    public class UIComponentTabContainer : UIComponentPanel, IDisposable
    {
        const string ADD = "AAAA";

        const string REMOVE = "BBBB";

        const string MOVE_LEFT = "CCCC";

        const string MOVE_RIGHT = "DDDD";

        const string RENAME = "EEEE";

        public const string Header = nameof(global::System.Windows.Controls.TabItem.Header);

        public UIComponentTabContainer()
        {
            this.EventHandlers = new Dictionary<UIComponentContainer, RoutedPropertyChangedEventHandler<UIComponentConfiguration>>();
            this.TabControl = new global::System.Windows.Controls.TabControl()
            {
                AllowDrop = true
            };
            TabControlExtensions.SetDragOverSelection(this.TabControl, true);
            TabControlExtensions.SetRightButtonSelect(this.TabControl, true);
            this.Content = this.TabControl;
            this.ContextMenu = new Menu()
            {
                Components = new ObservableCollection<IInvocableComponent>(new[] { this }),
                Source = this,
                ExplicitOrdering = true
            };
            this.ContextMenu.Opened += this.OnOpened;
        }

        protected virtual void OnOpened(object sender, RoutedEventArgs e)
        {
            var menu = sender as Menu;
            if (menu == null)
            {
                return;
            }
            menu.Source = this.TabControl.SelectedContent as UIComponentContainer;
        }

        public IDictionary<UIComponentContainer, RoutedPropertyChangedEventHandler<UIComponentConfiguration>> EventHandlers { get; private set; }

        public global::System.Windows.Controls.TabControl TabControl { get; private set; }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TabPanel)
            {
                var task = this.Add();
            }
            base.OnMouseDoubleClick(e);
        }

        protected override void OnConfigurationChanged()
        {
            this.UpdateChildren();
            base.OnConfigurationChanged();
        }

        protected virtual void UpdateChildren()
        {
            var index = this.TabControl.SelectedIndex;
            this.TabControl.Items.Clear(UIDisposerFlags.All);
            if (this.Configuration.Children.Count > 0)
            {
                foreach (var component in this.Configuration.Children)
                {
                    this.AddComponent(component);
                }
                if (index >= 0 && this.TabControl.Items.Count > index)
                {
                    this.TabControl.SelectedIndex = index;
                }
                else
                {
                    this.TabControl.SelectedIndex = 0;
                }
            }
            else
            {
                var component = new UIComponentConfiguration();
                this.AddComponent(component);
                this.Configuration.Children.Add(component);
            }
        }

        protected virtual void AddComponent(UIComponentConfiguration component)
        {
            var container = new UIComponentContainer()
            {
                Configuration = component
            };
            var eventHandler = new RoutedPropertyChangedEventHandler<UIComponentConfiguration>((sender, e) =>
            {
                this.UpdateComponent(component, container.Configuration);
            });
            container.ConfigurationChanged += eventHandler;
            this.EventHandlers.Add(container, eventHandler);
            var item = new TabItem()
            {
                Header = this.GetHeader(container),
                Content = container
            };
            this.TabControl.Items.Add(item);
        }

        protected virtual void UpdateComponent(UIComponentConfiguration originalComponent, UIComponentConfiguration newComponent)
        {
            for (var a = 0; a < this.Configuration.Children.Count; a++)
            {
                if (!object.ReferenceEquals(this.Configuration.Children[a], originalComponent))
                {
                    continue;
                }
                this.Configuration.Children[a] = newComponent;
                this.UpdateChildren();
                return;
            }
            //TODO: Component was not found.
            throw new NotImplementedException();
        }

        protected virtual void RemoveComponent(int position)
        {
            this.TabControl.Items.RemoveAt(position);
        }

        protected virtual void SwapComponents(int position1, int position2)
        {
            var tab1 = this.TabControl.Items[position1];
            var tab2 = this.TabControl.Items[position2];
            if (position1 > position2)
            {
                this.TabControl.Items.RemoveAt(position2);
                this.TabControl.Items.RemoveAt(position2);
                this.TabControl.Items.Insert(position2, tab2);
                this.TabControl.Items.Insert(position2, tab1);
            }
            else
            {
                this.TabControl.Items.RemoveAt(position1);
                this.TabControl.Items.RemoveAt(position1);
                this.TabControl.Items.Insert(position1, tab1);
                this.TabControl.Items.Insert(position1, tab2);
            }
        }

        protected virtual void OnComponentChanged(object sender, EventArgs e)
        {
            var container = sender as UIComponentContainer;
            if (container == null)
            {
                return;
            }
        }

        public override IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_GLOBAL;
            }
        }

        public override IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    ADD,
                    Strings.UIComponentTabContainer_Add
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    REMOVE,
                    Strings.UIComponentTabContainer_Remove
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    MOVE_LEFT,
                    Strings.UIComponentTabContainer_MoveLeft,
                    attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    MOVE_RIGHT,
                    Strings.UIComponentTabContainer_MoveRight
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_GLOBAL,
                    RENAME,
                    Strings.UIComponentTabContainer_Rename,
                    attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                );
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case ADD:
                    return this.Add();
            }
            if (component.Source is UIComponentContainer container)
            {
                switch (component.Id)
                {
                    case REMOVE:
                        return this.Remove(container);
                    case MOVE_LEFT:
                        return this.MoveLeft(container);
                    case MOVE_RIGHT:
                        return this.MoveRight(container);
                    case RENAME:
                        return this.Rename(container);
                }
            }
            return base.InvokeAsync(component);
        }

        public Task Add()
        {
            return Windows.Invoke(() =>
            {
                var component = new UIComponentConfiguration();
                this.Configuration.Children.Add(component);
                this.AddComponent(component);
                this.TabControl.SelectedIndex = this.TabControl.Items.Count - 1;
            });
        }

        public Task Remove(UIComponentContainer container)
        {
            return Windows.Invoke(() =>
            {
                for (var a = 0; a < this.Configuration.Children.Count; a++)
                {
                    if (!object.ReferenceEquals(this.Configuration.Children[a], container.Configuration))
                    {
                        continue;
                    }
                    this.Configuration.Children.RemoveAt(a);
                    if (this.Configuration.Children.Count == 0)
                    {
                        var task = this.Add();
                    }
                    this.UpdateChildren();
                    return;
                }
                //TODO: Component was not found.
                throw new NotImplementedException();
            });
        }

        public Task MoveLeft(UIComponentContainer container)
        {
            return Windows.Invoke(() =>
            {
                for (var a = 0; a < this.Configuration.Children.Count; a++)
                {
                    if (!object.ReferenceEquals(this.Configuration.Children[a], container.Configuration))
                    {
                        continue;
                    }
                    if (a > 0)
                    {
                        this.Configuration.Children[a] = this.Configuration.Children[a - 1];
                        this.Configuration.Children[a - 1] = container.Configuration;
                        this.SwapComponents(a, a - 1);
                        this.TabControl.SelectedIndex = a - 1;
                    }
                    return;
                }
                //TODO: Component was not found.
                throw new NotImplementedException();
            });
        }

        public Task MoveRight(UIComponentContainer container)
        {
            return Windows.Invoke(() =>
            {
                for (var a = 0; a < this.Configuration.Children.Count; a++)
                {
                    if (!object.ReferenceEquals(this.Configuration.Children[a], container.Configuration))
                    {
                        continue;
                    }
                    if (this.Configuration.Children.Count - 1 > a)
                    {
                        this.Configuration.Children[a] = this.Configuration.Children[a + 1];
                        this.Configuration.Children[a + 1] = container.Configuration;
                        this.SwapComponents(a, a + 1);
                        this.TabControl.SelectedIndex = a + 1;
                    }
                    return;
                }
                //TODO: Component was not found.
                throw new NotImplementedException();
            });
        }

        public Task Rename(UIComponentContainer container)
        {
            return Windows.Invoke(() =>
            {
                var header = InputBox.ShowDialog(Strings.UIComponentTabContainer_Rename, this.GetHeader(container));
                if (container.Configuration == null)
                {
                    container.Configuration = new UIComponentConfiguration();
                }
                if (!string.IsNullOrEmpty(header))
                {
                    container.Configuration.MetaData.AddOrUpdate(Header, header);
                }
                else
                {
                    container.Configuration.MetaData.TryRemove(Header);
                }
                for (var a = 0; a < this.Configuration.Children.Count; a++)
                {
                    if (!object.ReferenceEquals(this.Configuration.Children[a], container.Configuration))
                    {
                        continue;
                    }
                    var tab = this.TabControl.Items[a] as TabItem;
                    if (tab != null)
                    {
                        tab.Header = this.GetHeader(container);
                    }
                    break;
                }
            });
        }

        public string GetHeader(UIComponentContainer container)
        {
            var header = default(string);
            if (container.Configuration.MetaData.TryGetValue(Header, out header) && !string.IsNullOrEmpty(header))
            {
                return header;
            }
            if (!container.Configuration.Component.IsEmpty)
            {
                return container.Configuration.Component.Name;
            }
            return Strings.UIComponentTabContainer_NewTab;
        }

        protected override void OnDisposing()
        {
            if (this.EventHandlers != null)
            {
                foreach (var pair in this.EventHandlers)
                {
                    pair.Key.ConfigurationChanged -= pair.Value;
                }
            }
            if (this.TabControl != null)
            {
                foreach (var tabItem in this.TabControl.Items.Cast<TabItem>())
                {
                    if (object.ReferenceEquals(this.TabControl.SelectedItem, tabItem))
                    {
                        continue;
                    }
                    UIDisposer.Dispose(tabItem, UIDisposerFlags.All);
                }
            }
            base.OnDisposing();
        }
    }
}
